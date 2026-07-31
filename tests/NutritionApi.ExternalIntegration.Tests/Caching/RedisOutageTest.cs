namespace NutritionApi.ExternalIntegration.Tests.Caching;

using System.Net;
using NutritionApi.Domain.Entity;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// IT-EXT-15 — cache indisponible : la recherche repart en base plutôt que d'échouer.
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
    /// IT-EXT-15 — Redis arrêté, la recherche d'aliments répond 200 avec les données de PostgreSQL.
    /// </summary>
    [Fact]
    public async Task IT_EXT_15_CacheArrete_Retourne200DepuisLaBase()
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
