namespace NutritionApi.ExternalIntegration.Tests.Observability;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NutritionApi.ExternalIntegration.Tests.Fixtures;
using NutritionApi.Infrastructure;
using NutritionApi.Infrastructure.Observability;
using NutritionApi.Infrastructure.Persistence;

/// <summary>
/// Éprouve la supervision de la base (NTR-141) contre PostgreSQL réel.
/// </summary>
/// <remarks>
/// Trois choses qu'un test unitaire ne peut pas prouver : que l'intercepteur de requêtes lentes est
/// réellement branché sur le contexte EF Core configuré par <c>AddInfrastructure</c>, que les requêtes
/// de la relève correspondent au vrai schéma (<c>pg_stat_activity</c>, tables <c>hangfire</c>), et que
/// la relève ne se compte pas elle-même dans les connexions.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class DatabaseObservabilityTest(IntegrationFactory factory)
{
    /// <summary>
    /// l'intercepteur est branché sur le DbContext configuré par <c>AddInfrastructure</c>.
    /// </summary>
    /// <remarks>
    /// <c>SlowQueryInterceptor.Signaler</c> est déjà couvert par des cas de niveau 1 ; ce qui reste à
    /// éprouver, c'est qu'EF Core l'invoque. Le seuil est réglé à 1 ms et la commande réelle dure
    /// 50 ms (<c>pg_sleep(0.05)</c>) : l'avertissement ne peut venir que de l'intercepteur réellement
    /// attaché au contexte.
    /// </remarks>
    [Fact]
    public async Task SlowQueryInterceptor_EstBrancheSurLeDbContextDeAddInfrastructure()
    {
        var captures = new CapturingLoggerProvider();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = factory.Database.ConnectionString,
                ["DatabaseObservability:SlowQueryThresholdMs"] = "1"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddMetrics();
        services.AddLogging(builder => builder.AddProvider(captures).SetMinimumLevel(LogLevel.Warning));
        services.AddInfrastructure(configuration);

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await context.Database.ExecuteSqlRawAsync("select pg_sleep(0.05);");

        Assert.Contains(
            captures.Entrees,
            entree => entree.Category == typeof(SlowQueryInterceptor).FullName
                      && entree.Level == LogLevel.Warning
                      && entree.Message.Contains("Requête SQL lente"));
    }

    /// <summary>
    /// la relève lit le vrai schéma <c>hangfire</c> sans erreur de requête.
    /// </summary>
    /// <remarks>
    /// Les trois tables existent parce que Hangfire les a créées au démarrage de l'API. La requête
    /// catalogue doit les retrouver par leur nom, avec une taille disque non nulle et une estimation
    /// de lignes — c'est ce qui prouve qu'elle vise le bon schéma et les bonnes colonnes.
    /// </remarks>
    [Fact]
    public async Task CollecterAsync_LitLeVraiSchemaHangfire()
    {
        var service = new DatabaseSupervisionService(
            NullLogger<DatabaseSupervisionService>.Instance,
            factory.Services.GetRequiredService<InfrastructureMetrics>(),
            ConfigurationSur(factory.Database.ConnectionString));

        var releve = await service.CollecterAsync();

        Assert.Equal(3, releve.TablesHangfire.Count);
        Assert.Contains("job", releve.TablesHangfire.Keys);
        Assert.Contains("state", releve.TablesHangfire.Keys);
        Assert.Contains("counter", releve.TablesHangfire.Keys);

        foreach (var (table, lignes) in releve.TablesHangfire)
        {
            Assert.True(
                releve.TaillesDisqueHangfire[table] > 0,
                $"La table hangfire.{table} doit occuper une taille disque.");
            Assert.True(lignes >= 0, $"La table hangfire.{table} doit avoir une estimation de lignes.");
        }
    }

    /// <summary>
    /// la relève ne compte pas sa propre connexion.
    /// </summary>
    /// <remarks>
    /// Sur une base privée sans autre client, les trois compteurs doivent être nuls alors que la
    /// relève est elle-même en train d'exécuter sa requête. Sans « pid &lt;&gt; pg_backend_pid() »,
    /// elle se compterait dans l'état où elle se trouve et le test échouerait.
    /// </remarks>
    [Fact]
    public async Task CollecterAsync_NeComptePasSaPropreConnexion()
    {
        await using var database = await TestDatabase.CreateAsync();

        // Les migrations laissent une connexion inactive dans le pool de cette base : le vider pour
        // qu'il n'y ait plus aucun client, sans quoi le test mesurerait cette connexion-là.
        await using (var residuelle = new NpgsqlConnection(database.ConnectionString))
            NpgsqlConnection.ClearPool(residuelle);

        var service = new DatabaseSupervisionService(
            NullLogger<DatabaseSupervisionService>.Instance,
            factory.Services.GetRequiredService<InfrastructureMetrics>(),
            ConfigurationSur(database.ConnectionString));

        var releve = await service.CollecterAsync();

        // La fermeture des connexions vidées n'est pas instantanée côté serveur : on laisse à
        // PostgreSQL le temps de les retirer de pg_stat_activity avant de conclure.
        for (var tentative = 0; tentative < 20 && releve.ConnexionsInactives > 0; tentative++)
        {
            await Task.Delay(250);
            releve = await service.CollecterAsync();
        }

        Assert.Equal(0, releve.ConnexionsActives);
        Assert.Equal(0, releve.ConnexionsBloquees);
        Assert.Equal(0, releve.ConnexionsInactives);
    }

    /// <summary>Configuration minimale de la relève : la chaîne de connexion de la base visée.</summary>
    private static IConfiguration ConfigurationSur(string connectionString) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

    /// <summary>Journaliseur qui retient chaque entrée, pour la vérifier après coup.</summary>
    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly Lock _verrou = new();

        public List<Entree> Entrees { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, this);

        public void Dispose()
        {
        }

        private void Ajouter(Entree entree)
        {
            lock (_verrou)
            {
                Entrees.Add(entree);
            }
        }

        private sealed class CapturingLogger(string category, CapturingLoggerProvider fournisseur) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
                => fournisseur.Ajouter(new Entree(category, logLevel, formatter(state, exception)));
        }
    }

    /// <summary>Une entrée de journal retenue : sa catégorie, son niveau et son message mis en forme.</summary>
    private sealed record Entree(string Category, LogLevel Level, string Message);
}
