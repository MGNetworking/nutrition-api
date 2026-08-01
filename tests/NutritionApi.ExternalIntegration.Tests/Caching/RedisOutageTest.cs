namespace NutritionApi.ExternalIntegration.Tests.Caching;

using System.Net;
using Microsoft.EntityFrameworkCore;
using NutritionApi.Domain.Entity;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// cache indisponible : la recherche repart en base plutôt que d'échouer.
/// </summary>
/// <remarks>
/// Le repli est une promesse explicite du système : une panne du cache ne doit pas dégrader le
/// service rendu, seulement ses performances. Un 503 y serait disproportionné — l'application sait
/// répondre sans cache.
/// <para>
/// Les tests unitaires du service de cache travaillent sur un multiplexeur simulé : ils vérifient que
/// le code intercepte une exception qu'on lui fait lever. Ils ne disent pas qu'un Redis réellement
/// arrêté produit bien cette exception-là — et pas un blocage, ou un type que le <c>catch</c> ne
/// couvre pas.
/// </para>
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class RedisOutageTest(IntegrationFactory factory)
{
    /// <summary>
    /// Redis arrêté, la recherche d'aliments répond 200 avec les données de PostgreSQL.
    /// </summary>
    [Fact]
    public async Task GetFoodItems_ShouldReturn200FromDatabase_WhenCacheIsDown()
    {
        var keyword = $"itext15{Guid.NewGuid().ToString("N")[..8]}";
        await SeedFoodItemAsync(keyword);

        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.StandardUser);

        DockerContainer.Stop(DockerContainer.Redis);

        try
        {
            var response = await client.GetAsync($"/api/v1/food-items?search={keyword}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // La donnée vient nécessairement de PostgreSQL : le cache est arrêté, et elle n'y avait
            // de toute façon jamais été écrite.
            Assert.Contains(keyword, await response.Content.ReadAsStringAsync());
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Redis);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Redis, TimeSpan.FromSeconds(60));
        }
    }

    /// <summary>
    /// Redis arrêté en fin d'import : l'import se termine quand même.
    /// </summary>
    /// <remarks>
    /// L'import écrit les aliments en base, puis nettoie les recherches mémorisées. Ce nettoyage a
    /// longtemps laissé remonter une panne du cache, ce qui faisait échouer l'import entier — le
    /// planificateur retéléchargeait alors le dump et retraitait plusieurs millions de lignes, pour
    /// un geste de nettoyage. Les aliments étaient pourtant déjà en base.
    /// <para>
    /// Le prix assumé : les recherches mémorisées portent sur l'ancien catalogue jusqu'à
    /// l'expiration de leurs entrées, ou jusqu'au prochain import qui réussira son nettoyage. Une
    /// panne du cache relève de l'exploitation du système, pas de l'import.
    /// </para>
    /// <para>
    /// Ce cas ne peut vivre qu'au niveau 3 : un test unitaire ne prouverait que l'interception d'une
    /// exception qu'il fait lui-même lever, pas que Redis réellement arrêté produit bien celle-là.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task OffImportJob_ShouldComplete_WhenCacheIsDownAtInvalidation()
    {
        factory.DumpLines.Clear();
        factory.DumpLines.Add(DumpLine($"it-ext-24-{Guid.NewGuid()}", "Produit importé cache éteint"));

        DockerContainer.Stop(DockerContainer.Redis);

        try
        {
            var (scope, job) = factory.Resolve<IOffImportJob>();

            using (scope)
            {
                // L'absence d'exception est l'assertion : le planificateur ne voit aucun échec,
                // donc ne relance pas l'import.
                await job.RunAsync();
            }

            await using var context = factory.NewContext();

            Assert.Contains(
                await context.FoodItems.ToListAsync(),
                aliment => aliment.Name == "Produit importé cache éteint");
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Redis);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Redis, TimeSpan.FromSeconds(60));
        }
    }

    /// <summary>Construit une ligne JSONL au format du dump Open Food Facts.</summary>
    /// <param name="offId">Code-barres — unique, la colonne porte un index unique.</param>
    /// <param name="name">Nom du produit.</param>
    private static string DumpLine(string offId, string name) => $$$"""
        {"code":"{{{offId}}}","product_name":"{{{name}}}","allergens_tags":[],"nutriments":{"energy-kcal_100g":120,"proteins_100g":8,"carbohydrates_100g":15,"fat_100g":3}}
        """;

    /// <summary>Insère un aliment dont le nom contient le mot-clé recherché.</summary>
    private async Task SeedFoodItemAsync(string keyword)
    {
        await using var context = factory.NewContext();

        context.FoodItems.Add(new FoodItem(
            offId: $"{keyword}-{Guid.NewGuid()}",
            name: keyword,
            caloriesPer100g: 100f,
            proteinsPer100g: 10,
            carbsPer100g: 10,
            fatsPer100g: 10,
            allergensTags: []));

        await context.SaveChangesAsync();
    }
}
