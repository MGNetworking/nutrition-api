namespace NutritionApi.Integration.Tests.Fixtures;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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

    /// <summary>Base éphémère de cette exécution.</summary>
    public TestDatabase Database { get; }

    /// <summary>Jetons obtenus auprès du Keycloak réel.</summary>
    public KeycloakTokens Tokens { get; } = new();

    /// <summary>
    /// Lignes JSONL servies à la place du dump Open Food Facts. Un test qui déclenche l'import
    /// remplit cette liste ; vide, l'import n'importe rien — ce qu'exploite IT-EXT-12.
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
        => Database = Task.Run(TestDatabase.CreateAsync).GetAwaiter().GetResult();

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", Database.ConnectionString);
        builder.UseSetting("Redis:ConnectionString", $"{RedisEndpoint},defaultDatabase={RedisDatabaseIndex}");
        builder.UseSetting("Keycloak:Authority", KeycloakTokens.Authority);
        builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");

        // L'environnement « Testing » n'a pas de fichier de configuration : sans ces trois clés,
        // KeycloakAdminOptions resterait vide et le service d'administration construirait des URLs
        // inexploitables. Elles ne servent qu'au flux client_credentials — la validation des jetons,
        // elle, n'utilise aucune identité de client.
        builder.UseSetting("Keycloak:AdminBaseUrl", KeycloakTokens.AdminBaseUrl);
        builder.UseSetting("Keycloak:Realm", KeycloakTokens.Realm);
        builder.UseSetting("Keycloak:ServiceClientSecret", KeycloakTokens.ServiceClientSecret);

        // Le délai de production est de 60 s, pour absorber un démarrage simultané en cluster.
        // IT-EXT-17 attend cet échec : le raccourcir évite une minute d'attente à chaque exécution.
        builder.UseSetting("Keycloak:StartupTimeoutSeconds", "10");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IOffDumpReader>();
            services.AddSingleton<IOffDumpReader>(new ListDumpReader(DumpLines));
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

        Tokens.Dispose();

        if (Database is not null)
            await Database.DisposeAsync();
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
