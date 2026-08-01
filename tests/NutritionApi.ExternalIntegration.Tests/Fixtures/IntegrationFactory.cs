namespace NutritionApi.ExternalIntegration.Tests.Fixtures;

using Hangfire;
using Hangfire.AspNetCore;
using Hangfire.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Infrastructure.Persistence;

/// <summary>
/// Fabrique des tests de niveau 3 : l'application réelle, branchée sur les vrais PostgreSQL, Redis
/// et Keycloak du <c>docker-compose.yml</c>.
/// </summary>
/// <remarks>
/// À l'inverse de la fabrique de niveau 2, rien n'est remplacé aux frontières d'Application : ni
/// repository, ni cache, ni service Keycloak. Les <c>IHostedService</c> sont conservés — le serveur
/// Hangfire et l'enregistrement des jobs récurrents sont précisément ce que les cas IT-JOB-* doivent
/// éprouver.
/// <para>
/// Seule exception : <see cref="IOffDumpReader"/>, qui télécharge le dump Open Food Facts depuis
/// openfoodfacts.org. Ce n'est ni PostgreSQL, ni Redis, ni Keycloak — c'est un tiers de plusieurs
/// gigaoctets, hors du périmètre de ce niveau. Il est remplacé par une source de lignes alimentée
/// par le test, ce qui laisse réels le mapping, la persistance par lots et l'invalidation du cache.
/// </para>
/// </remarks>
public sealed class IntegrationFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Index de la base Redis réservée aux tests — isole le cache sans exiger une seconde instance.
    /// Le développement utilise l'index 0.
    /// </summary>
    private static string RedisDatabaseIndex =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_REDIS_DB") ?? "9";

    /// <summary>Point de connexion Redis — surchargeable par <c>NUTRITION_TEST_REDIS</c> en CI.</summary>
    private static string RedisEndpoint =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_REDIS") ?? "localhost:6336";

    /// <summary>
    /// Première fabrique construite dans le processus — celle que porte la collection, partagée par
    /// tous les cas. Les fabriques créées ensuite par un test sont secondaires et éphémères.
    /// </summary>
    private static IntegrationFactory? _partagee;

    /// <summary>Vrai tant que la fabrique partagée n'a pas été libérée.</summary>
    private static bool _partageeVivante;

    /// <summary>Base éphémère de cette exécution.</summary>
    public TestDatabase Database { get; }

    /// <summary>Jetons obtenus auprès du Keycloak réel.</summary>
    public KeycloakTokens Tokens { get; } = new();

    /// <summary>
    /// Lignes JSONL servies à la place du dump Open Food Facts. Un test qui déclenche l'import
    /// remplit cette liste ; vide, l'import n'importe rien — ce qu'exploitent les cas d'invalidation.
    /// </summary>
    public List<string> DumpLines { get; } = [];

    /// <summary>
    /// Crée la base et applique les migrations. Le travail a lieu dans le constructeur parce que
    /// <see cref="ConfigureWebHost"/> lit la chaîne de connexion au premier client créé : elle doit
    /// exister avant. <c>IAsyncLifetime</c> de xUnit ne convient pas, sa méthode de libération entre
    /// en conflit avec celle de <see cref="WebApplicationFactory{TEntryPoint}"/>.
    /// </summary>
    /// <remarks>
    /// L'attente est déportée sur le pool de threads : bloquer directement sur le contexte de
    /// synchronisation de xUnit exposerait à un interblocage.
    /// </remarks>
    public IntegrationFactory()
    {
        Database = Task.Run(TestDatabase.CreateAsync).GetAwaiter().GetResult();

        if (_partagee is not null)
            return;

        _partagee = this;
        _partageeVivante = true;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Environnement propre au niveau 3. Le niveau 2 utilise « Testing » et pointe vers une
        // autorité factice : un appsettings.Testing.json le ferait viser le vrai Keycloak, la
        // configuration applicative primant sur les UseSetting de sa fabrique.
        builder.UseEnvironment("ExternalIntegration");

        // Ce qui change à chaque exécution ou selon la machine reste ici.
        builder.UseSetting("ConnectionStrings:DefaultConnection", Database.ConnectionString);
        builder.UseSetting("Redis:ConnectionString", $"{RedisEndpoint},defaultDatabase={RedisDatabaseIndex}");

        // Ce qui décrit le realm est lu dans keycloak/realm-export.json, jamais ressaisi : le placer
        // dans appsettings.ExternalIntegration.json recréerait la duplication qu'on supprime.
        builder.UseSetting("Keycloak:Authority", KeycloakTokens.Authority);
        builder.UseSetting("Keycloak:AdminBaseUrl", KeycloakTokens.AdminBaseUrl);
        builder.UseSetting("Keycloak:Realm", KeycloakTokens.Realm);
        builder.UseSetting("Keycloak:ServiceClientSecret", KeycloakTokens.ServiceClientSecret);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOffDumpReader>();
            services.AddSingleton<IOffDumpReader>(new ListDumpReader(DumpLines));

            // La tolérance d'horloge par défaut de la validation JWT est de cinq minutes : elle
            // absorbe les décalages entre le serveur d'identité et l'API. Un jeton expiré depuis une
            // seconde resterait donc accepté, et le test d'expiration ne pourrait rien prouver sans attendre
            // plus de cinq minutes. Elle est annulée ici, et ici seulement — la production conserve
            // la valeur par défaut.
            services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.TokenValidationParameters.ClockSkew = TimeSpan.Zero);
        });
    }

    /// <summary>Crée un client portant un jeton réellement émis par Keycloak.</summary>
    /// <param name="username">Compte du realm — <see cref="KeycloakTokens.StandardUser"/> par défaut.</param>
    /// <returns>Un client HTTP dont les requêtes traversent la validation JWT complète.</returns>
    public async Task<HttpClient> CreateTokenClientAsync(string? username = null)
    {
        var token = await Tokens.GetAccessTokenAsync(username ?? KeycloakTokens.StandardUser);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);

        return client;
    }

    /// <summary>Ouvre un contexte EF Core sur la base de cette exécution.</summary>
    /// <returns>Un contexte à disposer par l'appelant.</returns>
    public AppDbContext NewContext() => Database.NewContext();

    /// <summary>Résout un service dans une portée dédiée.</summary>
    /// <typeparam name="TService">Service à résoudre.</typeparam>
    /// <returns>La portée et le service — la portée est à disposer par l'appelant.</returns>
    public (IServiceScope Scope, TService Service) Resolve<TService>()
        where TService : notnull
    {
        var scope = Services.CreateScope();

        return (scope, scope.ServiceProvider.GetRequiredService<TService>());
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        // L'hôte se ferme d'abord : le serveur Hangfire et le pool Npgsql gardent sinon des
        // connexions ouvertes, et PostgreSQL refuserait la suppression de la base.
        await base.DisposeAsync();

        if (ReferenceEquals(this, _partagee))
            _partageeVivante = false;
        else
            RendreLesStatiquesHangfireALaFabriquePartagee();

        Tokens.Dispose();

        if (Database is not null)
            await Database.DisposeAsync();
    }

    /// <summary>
    /// Repointe les statiques de Hangfire sur la fabrique partagée, après la libération d'une
    /// fabrique secondaire.
    /// </summary>
    /// <remarks>
    /// Hangfire retient deux références <b>de processus</b>, réécrites par chaque hôte construit :
    /// <see cref="JobStorage.Current"/> et le fournisseur de journaux de <see cref="LogProvider"/>.
    /// Un second hôte les fait basculer sur les siens ; sa libération laisse Hangfire tenir un
    /// <c>ILoggerFactory</c> disposé et un storage dont la base est supprimée.
    /// <para>
    /// La conséquence était un job perdu, et NTR-156 en entier : le worker du serveur partagé
    /// dépilait un job — transaction validée, <c>fetchedat</c> écrit — puis échouait sur
    /// <c>ObjectDisposedException</c> en construisant le job dépilé, qui demande un logger. Aucun
    /// objet ne subsistait pour remettre le job en file : il restait invisible trente minutes, sans
    /// état <c>Processing</c>, sans échec, pendant que le serveur battait normalement.
    /// </para>
    /// <para>
    /// La production n'est pas concernée : un processus n'y héberge qu'une application, dont le
    /// <c>ILoggerFactory</c> vit aussi longtemps qu'elle.
    /// </para>
    /// </remarks>
    private static void RendreLesStatiquesHangfireALaFabriquePartagee()
    {
        if (_partagee is null || !_partageeVivante)
            return;

        JobStorage.Current = _partagee.Services.GetRequiredService<JobStorage>();

        LogProvider.SetCurrentLogProvider(
            new AspNetCoreLogProvider(_partagee.Services.GetRequiredService<ILoggerFactory>()));
    }

    /// <summary>Source de dump alimentée en mémoire, à la place du téléchargement réel.</summary>
    /// <param name="lines">Lignes JSONL à servir — lues à chaque exécution du job.</param>
    private sealed class ListDumpReader(List<string> lines) : IOffDumpReader
    {
        /// <inheritdoc />
        public async IAsyncEnumerable<string> ReadLinesAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            foreach (var line in lines)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return line;
            }

            await Task.CompletedTask;
        }
    }
}
