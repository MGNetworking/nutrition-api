namespace NutritionApi.Integration.Tests.Fixtures;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NutritionApi.Infrastructure.Persistence;
using NutritionApi.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Base PostgreSQL dédiée à une exécution de la suite, créée sur le conteneur de développement.
/// </summary>
/// <remarks>
/// Le ticket NTR-28 demande une base isolée par session. Plutôt qu'un second fichier compose, on
/// réutilise <c>nutrition-postgres</c> — celui que <c>scripts/dev-up.sh</c> monte déjà — et on y
/// crée une base éphémère. Un seul jeu de scripts sert donc au développement et aux tests, en local
/// comme en CI.
/// <para>
/// L'isolation est réelle : deux exécutions concurrentes ne partagent aucune table, et
/// <c>nutrition_dev</c> avec ses données de démonstration n'est jamais touchée.
/// </para>
/// </remarks>
public sealed class TestDatabase : IAsyncDisposable
{
    /// <summary>Connexion d'administration — la base <c>postgres</c> est toujours présente.</summary>
    /// <remarks>
    /// Surchargeable par la variable d'environnement <c>NUTRITION_TEST_POSTGRES</c> : la CI peut
    /// ainsi viser un autre hôte ou un autre port sans modifier le code.
    /// </remarks>
    private static string AdminConnectionString =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_POSTGRES")
        ?? "Host=localhost;Port=5445;Database=postgres;Username=postgres;Password=postgres";

    /// <summary>Nom de la base créée pour cette exécution.</summary>
    public string Name { get; }

    /// <summary>Chaîne de connexion à la base de cette exécution.</summary>
    public string ConnectionString { get; }

    private TestDatabase(string name, string connectionString)
    {
        Name = name;
        ConnectionString = connectionString;
    }

    /// <summary>Crée la base et y applique les migrations EF Core.</summary>
    /// <returns>La base prête à l'emploi.</returns>
    /// <exception cref="InvalidOperationException">PostgreSQL est injoignable — la pile Docker n'est pas montée.</exception>
    public static async Task<TestDatabase> CreateAsync()
    {
        // Horodatage à la milliseconde plus un suffixe court : deux exécutions lancées dans la même
        // milliseconde (CI parallèle) ne peuvent pas se retrouver sur le même nom.
        var name = $"nutrition_test_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid().ToString("N")[..6]}";

        var builder = new NpgsqlConnectionStringBuilder(AdminConnectionString);

        await using (var admin = new NpgsqlConnection(builder.ConnectionString))
        {
            try
            {
                await admin.OpenAsync();
            }
            catch (NpgsqlException exception)
            {
                throw new InvalidOperationException(
                    $"PostgreSQL est injoignable sur {builder.Host}:{builder.Port}. "
                    + "Monter la pile avec ./scripts/dev-up.sh avant de lancer les tests de niveau 3.",
                    exception);
            }

            // Le nom est construit ici, pas fourni par l'appelant : pas de paramètre à échapper.
            await using var create = new NpgsqlCommand($"create database \"{name}\";", admin);
            await create.ExecuteNonQueryAsync();
        }

        builder.Database = name;
        var connectionString = builder.ConnectionString;

        await using (var context = NewContext(connectionString))
            await context.Database.MigrateAsync();

        return new TestDatabase(name, connectionString);
    }

    /// <summary>
    /// Construit un contexte EF Core sur cette base, configuré comme en production —
    /// convention snake_case et intercepteur de traduction des erreurs compris.
    /// </summary>
    /// <returns>Un contexte à disposer par l'appelant.</returns>
    /// <remarks>
    /// L'intercepteur est présent volontairement : IT-EXT-13 vérifie précisément qu'EF Core
    /// l'invoque. Un contexte de test qui l'omettrait ne prouverait rien.
    /// </remarks>
    public AppDbContext NewContext() => NewContext(ConnectionString);

    private static AppDbContext NewContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new DatabaseExceptionInterceptor())
            .UseLoggerFactory(NullLoggerFactory.Instance)
            .Options;

        return new AppDbContext(options);
    }

    /// <summary>Supprime la base de cette exécution.</summary>
    /// <remarks>
    /// Les connexions ouvertes sont d'abord coupées : PostgreSQL refuse de supprimer une base
    /// encore utilisée, et le pool Npgsql garde des connexions au repos après le dernier test.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var admin = new NpgsqlConnection(AdminConnectionString);
        await admin.OpenAsync();

        await using var drop = new NpgsqlCommand(
            $"drop database if exists \"{Name}\" with (force);", admin);

        await drop.ExecuteNonQueryAsync();
    }
}
