namespace NutritionApi.Api.Tests.Level2;

using Moq;
using NutritionApi.Api.Tests.Level2.Fixtures;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>FoodItemsController</c> — NTR-110. Couvre IT-FD-01, IT-FD-03 et IT-FD-04.
/// </summary>
/// <remarks>
/// IT-FD-02 (cache miss → PostgreSQL → écriture Redis avec TTL) n'est pas ici : l'orchestration
/// réelle entre le cache et la base est indémontrable avec des doublures. Elle relève du niveau 3
/// (NTR-73).
/// </remarks>
[Trait("Level", "2")]
[Collection(ApiCollection.Name)]
public class FoodItemsIntegrationTest
{
    private const string Endpoint = "/api/v1/food-items";

    private static readonly List<FoodItemSearchResponse> Poulet =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Poulet rôti", 165f, 31f, 0f, 3.6f, [])
    ];

    private readonly ApiFactory _factory;

    public FoodItemsIntegrationTest(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetInvocations();
    }

    [Fact]
    public async Task IT_FD_01_CacheDisponible_Retourne200SansInterrogerLeCatalogue()
    {
        _factory.FoodCache.Setup(c => c.GetAsync("poulet")).ReturnsAsync(Poulet);
        _factory.FoodItems.Invocations.Clear();

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}?search=poulet");
        var resultats = await reponse.Content.ReadFromJsonAsync<List<FoodItemSearchResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Poulet rôti", Assert.Single(resultats!).Name);

        // Le cache a répondu : le catalogue ne doit pas avoir été sollicité.
        _factory.FoodItems.Verify(
            r => r.SearchByKeywordAsync(It.IsAny<string>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task IT_FD_03_LimiteFournie_TronqueLaReponse()
    {
        var douzeAliments = Enumerable.Range(0, 12)
            .Select(i => new FoodItemSearchResponse(Guid.NewGuid(), $"Aliment {i}", 100f, 10f, 10f, 5f, []))
            .ToList();
        _factory.FoodCache.Setup(c => c.GetAsync("poulet")).ReturnsAsync(douzeAliments);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}?search=poulet&limit=5");
        var resultats = await reponse.Content.ReadFromJsonAsync<List<FoodItemSearchResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(5, resultats!.Count);
    }

    [Fact]
    public async Task CacheMiss_InterrogeLeCatalogueSansTenirCompteDeLaLimite()
    {
        var limiteRecue = 0;
        _factory.FoodCache.Setup(c => c.GetAsync("brocoli")).ReturnsAsync((List<FoodItemSearchResponse>?)null);
        _factory.FoodItems
            .Setup(r => r.SearchByKeywordAsync("brocoli", It.IsAny<int>()))
            .Callback<string, int>((_, limit) => limiteRecue = limit)
            .ReturnsAsync([]);

        await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}?search=brocoli&limit=5");

        // Choix délibéré du service : le catalogue est toujours interrogé sur 20 résultats, et
        // c'est ce jeu complet qui est mis en cache. La troncature n'intervient qu'en sortie, pour
        // qu'une entrée de cache serve toutes les valeurs de limit.
        Assert.Equal(20, limiteRecue);
    }

    [Fact]
    public async Task IT_FD_04_SansParametreSearch_Retourne400()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Recherche_SansIdentite_Retourne401()
    {
        var reponse = await _factory.CreateAnonymousClient().GetAsync($"{Endpoint}?search=poulet");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }
}
