namespace NutritionApi.Api.Tests.Level2;

using Moq;
using NutritionApi.Api.Tests.Level2.Fixtures;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>FoodItemsController</c> — NTR-110. Couvre la recherche servie par le
/// cache, le repli sur le catalogue, la troncature et le paramètre manquant.
/// </summary>
/// <remarks>
/// L'orchestration réelle entre le cache et la base — écriture Redis, durée de vie, expiration —
/// n'est pas ici : elle est indémontrable avec des doublures et relève du niveau 3 (NTR-73).
/// Ce fichier vérifie ce qui l'est : que le catalogue est interrogé quand le cache ne répond pas,
/// et que le résultat y est remis.
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
    public async Task GetFoodItems_ShouldReturn200WithoutQueryingCatalog_WhenCacheIsWarm()
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
    public async Task GetFoodItems_ShouldTruncateResults_WhenLimitIsProvided()
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

    /// <summary>
    /// Cache vide : le catalogue est interrogé, et son résultat est remis en cache.
    /// </summary>
    /// <remarks>
    /// C'est le chemin inverse de la recherche servie par le cache, et il manquait. Sans lui, rien
    /// ne vérifiait que le service alimente le cache après un défaut : la recherche suivante serait
    /// repartie en base, indéfiniment.
    /// <para>
    /// Ce que ce niveau peut prouver s'arrête là — que <c>SetAsync</c> est appelé avec les résultats.
    /// Qu'ils soient réellement écrits dans Redis, avec leur durée de vie, relève du niveau 3.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task GetFoodItems_ShouldQueryCatalogAndFillCache_WhenCacheIsEmpty()
    {
        _factory.FoodCache.Setup(c => c.GetAsync("brocoli")).ReturnsAsync((List<FoodItemSearchResponse>?)null);
        _factory.FoodItems
            .Setup(r => r.SearchByKeywordAsync("brocoli", It.IsAny<int>()))
            .ReturnsAsync([new FoodItem("3017620422003", "Brocoli", 34f, 3, 7, 0, [])]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}?search=brocoli");
        var resultats = await reponse.Content.ReadFromJsonAsync<List<FoodItemSearchResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Brocoli", Assert.Single(resultats!).Name);

        // Le catalogue a été interrogé, et le résultat remis en cache pour la recherche suivante.
        _factory.FoodItems.Verify(r => r.SearchByKeywordAsync("brocoli", It.IsAny<int>()), Times.Once);
        _factory.FoodCache.Verify(
            c => c.SetAsync("brocoli", It.Is<List<FoodItemSearchResponse>>(l => l.Count == 1)),
            Times.Once);
    }

    /// <summary>
    /// Cache vide : le catalogue est interrogé sur le jeu complet, indépendamment de la limite.
    /// </summary>
    [Fact]
    public async Task GetFoodItems_ShouldQueryCatalogWithFullLimit_WhenCacheIsEmpty()
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
    public async Task GetFoodItems_ShouldReturn400_WhenSearchParameterIsMissing()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Endpoint);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task GetFoodItems_ShouldReturn401_WhenIdentityIsAbsent()
    {
        var reponse = await _factory.CreateAnonymousClient().GetAsync($"{Endpoint}?search=poulet");

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }
}
