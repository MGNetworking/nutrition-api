using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NutritionApi.Infrastructure.Persistence;

/// <summary>Fabrique du DbContext pour les outils EF Core en design-time (génération de migrations).</summary>
/// <remarks>
/// Utilisée uniquement par <c>dotnet ef</c> — jamais au runtime. La chaîne de connexion n'est pas
/// utilisée pour <c>migrations add</c> ; elle peut être surchargée via la variable d'environnement
/// <c>NUTRITION_DB_CONNECTION</c> pour <c>database update</c>.
/// Les options doivent rester identiques à l'enregistrement DI du runtime (Npgsql + snake_case),
/// sans quoi les migrations générées ne correspondraient pas au schéma réel.
/// </remarks>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("NUTRITION_DB_CONNECTION")
            ?? "Host=localhost;Database=nutrition;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options);
    }
}
