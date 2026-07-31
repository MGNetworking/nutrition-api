namespace NutritionApi.ExternalIntegration.Tests.Jobs;

using Microsoft.EntityFrameworkCore;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Infrastructure.Jobs.RgpdPurge;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// exécution réelle des jobs planifiés contre la base.
/// </summary>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class JobExecutionTest(IntegrationFactory factory)
{
    /// <summary>
    /// l'import déclenché manuellement persiste les aliments du dump.
    /// </summary>
    /// <remarks>
    /// Le téléchargement depuis openfoodfacts.org est remplacé par une source de lignes en mémoire
    /// (voir <c>IntegrationFactory</c>) : ce n'est ni PostgreSQL, ni Redis, ni Keycloak, et le dump
    /// réel pèse plusieurs gigaoctets. Restent réels le mapping, la persistance par lots et l'index
    /// unique sur <c>off_id</c>.
    /// </remarks>
    [Fact]
    public async Task OffImportJob_ShouldPersistFoodItems_WhenTriggeredManually()
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
    /// la purge RGPD déclenchée manuellement supprime le compte en base.
    /// </summary>
    /// <remarks>
    /// Le job supprime Keycloak d'abord — <c>DeleteUserAsync(user.KeycloakId)</c> — puis la ligne en
    /// base. Les deux suppressions sont donc vérifiées.
    /// <para>
    /// <b>Le compte Keycloak est réellement créé.</b> <c>keycloak_id</c> est par définition le
    /// <c>sub</c> d'un compte existant : inventer cette valeur construirait un état impossible en
    /// production, et l'appel de suppression emprunterait la branche « compte déjà absent » — un 404
    /// que <c>KeycloakAdminService</c> traite comme un succès. Le test passerait sans jamais éprouver
    /// la suppression.
    /// </para>
    /// <para>
    /// Le compte est jetable et créé pour ce seul test : les comptes du realm servent aux autres cas,
    /// les supprimer les casserait.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task RgpdPurgeJob_ShouldDeleteAccountFromKeycloakAndDatabase_WhenGracePeriodExpired()
    {
        var username = $"it-ext-08-{Guid.NewGuid():N}";
        var keycloakId = await factory.Tokens.CreateThrowawayUserAsync(username);

        try
        {
            Assert.True(
                await factory.Tokens.UserExistsAsync(keycloakId),
                "Le compte jetable doit exister avant la purge, sinon le test ne prouve rien.");

            var user = await SeedUserAsync(keycloakId);

            // La grace period est de 30 jours et MarkAsDeleted fixe DeletedAt à maintenant : la date
            // est reculée en SQL, seul moyen de rendre le compte éligible sans attendre un mois.
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
            Assert.False(
                await factory.Tokens.UserExistsAsync(keycloakId),
                "Le compte devait disparaître de Keycloak, pas seulement de la base.");
        }
        finally
        {
            // Le test peut échouer avant la purge : ne pas laisser de compte derrière soi.
            await factory.Tokens.DeleteUserAsync(keycloakId);
        }
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
