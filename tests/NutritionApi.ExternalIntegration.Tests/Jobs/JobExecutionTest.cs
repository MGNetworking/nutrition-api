namespace NutritionApi.ExternalIntegration.Tests.Jobs;

using Microsoft.EntityFrameworkCore;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Infrastructure.Jobs.RgpdPurge;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// IT-EXT-07 et IT-EXT-08 — exécution réelle des jobs planifiés contre la base.
/// </summary>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class JobExecutionTest(IntegrationFactory factory)
{
    /// <summary>
    /// IT-EXT-07 — l'import déclenché manuellement persiste les aliments du dump.
    /// </summary>
    /// <remarks>
    /// Le téléchargement depuis openfoodfacts.org est remplacé par une source de lignes en mémoire
    /// (voir <c>IntegrationFactory</c>) : ce n'est ni PostgreSQL, ni Redis, ni Keycloak, et le dump
    /// réel pèse plusieurs gigaoctets. Restent réels le mapping, la persistance par lots et l'index
    /// unique sur <c>off_id</c>.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_07_ImportManuel_PersisteLesAliments()
    {
        var premier = $"it-ext-07-a-{Guid.NewGuid()}";
        var second = $"it-ext-07-b-{Guid.NewGuid()}";

        factory.DumpLines.Clear();
        factory.DumpLines.Add(DumpLine(premier, "Yaourt nature de test"));
        factory.DumpLines.Add(DumpLine(second, "Pain complet de test"));

        var (scope, job) = factory.Resolve<IOffImportJob>();

        using (scope)
            await job.RunAsync();

        await using var context = factory.NewContext();

        Assert.NotNull(await context.FoodItems.FirstOrDefaultAsync(f => f.OffId == premier));
        Assert.NotNull(await context.FoodItems.FirstOrDefaultAsync(f => f.OffId == second));
    }

    /// <summary>
    /// IT-EXT-08 — la purge RGPD déclenchée manuellement supprime le compte en base.
    /// </summary>
    /// <remarks>
    /// Le job supprime Keycloak d'abord, via le flux <c>client_credentials</c> du client confidentiel
    /// <c>nutrition-api-service</c>. Ce test éprouve donc la chaîne complète : obtention du jeton de
    /// service, appel à l'API d'administration, puis suppression en base.
    /// <para>
    /// L'identifiant Keycloak semé n'existe pas dans le realm : l'appel de suppression répond 404, que
    /// <c>KeycloakAdminService</c> traite comme « le compte n'existe plus » et ignore. C'est ce qui
    /// rend la purge idempotente, et ce qui permet à ce test de ne pas consommer un compte du realm.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task IT_EXT_08_PurgeManuelle_SupprimeLeCompte()
    {
        var keycloakId = $"it-ext-08-{Guid.NewGuid()}";
        var user = await SeedUserAsync(keycloakId);

        // La grace period est de 30 jours et MarkAsDeleted fixe DeletedAt à maintenant : la date est
        // reculée en SQL, seul moyen de rendre le compte éligible sans attendre un mois.
        await using (var context = factory.NewContext())
        {
            await context.Database.ExecuteSqlAsync(
                $"update users set deleted_at = now() - interval '40 days' where id = {user.Id}");
        }

        var (scope, job) = factory.Resolve<IRgpdPurgeJob>();

        using (scope)
            await job.RunAsync();

        await using var verification = factory.NewContext();

        Assert.Null(await verification.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId));
    }

    /// <summary>Construit une ligne JSONL au format du dump Open Food Facts.</summary>
    private static string DumpLine(string offId, string name) => $$$"""
        {"code":"{{{offId}}}","product_name":"{{{name}}}","allergens_tags":[],"nutriments":{"energy-kcal_100g":95,"proteins_100g":4,"carbohydrates_100g":12,"fat_100g":3}}
        """;

    private async Task<User> SeedUserAsync(string keycloakId)
    {
        await using var context = factory.NewContext();

        var user = new User(
            keycloakId: keycloakId,
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            height: 180,
            allergies: [],
            dietaryPreferences: []);

        user.MarkAsDeleted();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }
}
