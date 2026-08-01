using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace NutritionApi.Infrastructure.Persistence;

/// <summary>Fabrique du DbContext pour les outils EF Core en design-time (génération de migrations).</summary>
/// <remarks>
/// Utilisée uniquement par <c>dotnet ef</c> — jamais au runtime, où le DbContext est fourni par le DI.
/// La chaîne de connexion est lue dans la configuration du projet de démarrage, ou surchargée par la
/// variable d'environnement <c>NUTRITION_DB_CONNECTION</c> — aucune valeur n'est codée en dur.
/// Les options doivent rester identiques à l'enregistrement DI du runtime (Npgsql + snake_case),
/// sans quoi les migrations générées ne correspondraient pas au schéma réel.
/// </remarks>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString =
            Environment.GetEnvironmentVariable("NUTRITION_DB_CONNECTION")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Chaîne de connexion introuvable. Lancer la commande depuis le projet de démarrage " +
                "(--startup-project src/NutritionApi.Api), renseigner ConnectionStrings:DefaultConnection " +
                "dans appsettings.Development.json, ou définir la variable NUTRITION_DB_CONNECTION.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
