namespace NutritionApi.Integration.Tests.Caching;

using Microsoft.Extensions.DependencyInjection;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Integration.Tests.Fixtures;
using StackExchange.Redis;

/// <summary>
/// IT-EXT-05, 06, 11 et 12 — le cache Redis réel et son invalidation par l'import (sous-tâche NTR-73).
/// </summary>
/// <remarks>
/// Les tests unitaires du cache travaillent sur une doublure de <c>IConnectionMultiplexer</c> : ils
/// vérifient que le service appelle Redis, pas que Redis se comporte comme attendu. L'expiration
/// réelle d'une clé et le parcours par motif de <c>InvalidateAllSearchesAsync</c> n'existent qu'ici.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class RedisCacheTest(IntegrationFactory factory)
{
    /// <summary>
    /// Motif des clés de recherche, tel que le construit <c>RedisFoodCacheService</c>.
    /// </summary>
    /// <remarks>
    /// Reconstruit ici parce que le préfixe et la version de schéma sont des constantes privées du
    /// service. Si l'une des deux change, ces tests échouent — c'est voulu : la forme des clés est un
    /// contrat que l'invalidation par motif dépend de connaître.
    /// </remarks>
    private const string KeyPattern = "food:search:*";

    private static string KeyOf(string keyword) => $"food:search:v1:{keyword.ToLowerInvariant().Trim()}";

    /// <summary>
    /// IT-EXT-05 — première recherche en base et mise en cache, seconde recherche servie par Redis.
    /// </summary>
    /// <remarks>
    /// La preuve du <i>hit</i> est obtenue en supprimant l'aliment de la base entre les deux appels :
    /// si la seconde recherche renvoie encore un résultat, il ne peut venir que du cache.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_05_Cache_miss_puis_hit()
    {
        var keyword = $"itext05{Guid.NewGuid().ToString("N")[..8]}";
        var foodItem = await SeedFoodItemAsync(keyword);

        var database = Redis().GetDatabase();
        await database.KeyDeleteAsync(KeyOf(keyword));

        var (firstScope, service) = factory.Resolve<IFoodItemService>();
        using (firstScope)
        {
            var premier = await service.SearchAsync(keyword);

            Assert.NotEmpty(premier);
            Assert.True(await database.KeyExistsAsync(KeyOf(keyword)), "La recherche doit avoir été mise en cache.");
        }

        await using (var context = factory.NewContext())
        {
            context.FoodItems.Remove(await context.FoodItems.FindAsync(foodItem.Id) ?? foodItem);
            await context.SaveChangesAsync();
        }

        var (secondScope, cachedService) = factory.Resolve<IFoodItemService>();
        using (secondScope)
        {
            var second = await cachedService.SearchAsync(keyword);

            Assert.NotEmpty(second);
        }
    }

    /// <summary>
    /// IT-EXT-06 — une fois la clé expirée, la recherche repart en base.
    /// </summary>
    /// <remarks>
    /// La durée de vie configurée est de 24 heures : elle n'est pas attendue. La clé reçoit une
    /// expiration d'une seconde, et c'est bien Redis qui la fait disparaître — ce que ce niveau doit
    /// prouver. L'aliment est supprimé en parallèle : après expiration, la recherche doit revenir
    /// vide, donc avoir réellement interrogé PostgreSQL.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_06_Expiration_du_ttl_renvoie_en_base()
    {
        var keyword = $"itext06{Guid.NewGuid().ToString("N")[..8]}";
        var foodItem = await SeedFoodItemAsync(keyword);

        var database = Redis().GetDatabase();

        var (scope, service) = factory.Resolve<IFoodItemService>();
        using (scope)
        {
            Assert.NotEmpty(await service.SearchAsync(keyword));
        }

        await using (var context = factory.NewContext())
        {
            context.FoodItems.Remove(await context.FoodItems.FindAsync(foodItem.Id) ?? foodItem);
            await context.SaveChangesAsync();
        }

        await database.KeyExpireAsync(KeyOf(keyword), TimeSpan.FromSeconds(1));
        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.False(await database.KeyExistsAsync(KeyOf(keyword)), "Redis devait avoir expiré la clé.");

        var (expiredScope, expiredService) = factory.Resolve<IFoodItemService>();
        using (expiredScope)
        {
            Assert.Empty(await expiredService.SearchAsync(keyword));
        }
    }

    /// <summary>
    /// IT-EXT-11 — un import ayant importé au moins un produit vide les recherches en cache.
    /// </summary>
    [Fact]
    public async Task IT_EXT_11_Import_avec_produits_vide_le_cache()
    {
        var keyword = $"itext11{Guid.NewGuid().ToString("N")[..8]}";
        var database = Redis().GetDatabase();

        await database.StringSetAsync(KeyOf(keyword), "[]", TimeSpan.FromMinutes(5));

        factory.DumpLines.Clear();
        factory.DumpLines.Add(DumpLine($"it-ext-11-{Guid.NewGuid()}", "Produit importé"));

        await RunImportAsync();

        Assert.False(
            await database.KeyExistsAsync(KeyOf(keyword)),
            "L'import ayant modifié le catalogue, les recherches en cache devaient être supprimées.");
    }

    /// <summary>
    /// IT-EXT-12 — un import n'ayant rien importé laisse le cache intact.
    /// </summary>
    /// <remarks>
    /// Le catalogue n'a pas changé : vider le cache ferait repartir toutes les recherches en base
    /// sans raison. La source de dump est vide, l'import ne persiste donc rien.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_12_Import_sans_produit_laisse_le_cache_intact()
    {
        var keyword = $"itext12{Guid.NewGuid().ToString("N")[..8]}";
        var database = Redis().GetDatabase();

        await database.StringSetAsync(KeyOf(keyword), "[]", TimeSpan.FromMinutes(5));

        factory.DumpLines.Clear();

        await RunImportAsync();

        Assert.True(
            await database.KeyExistsAsync(KeyOf(keyword)),
            "Aucun produit importé : le cache devait rester en place.");
    }

    /// <summary>Multiplexeur Redis de l'application — même instance que celle du code testé.</summary>
    private IConnectionMultiplexer Redis() => factory.Services.GetRequiredService<IConnectionMultiplexer>();

    /// <summary>Déclenche l'import Open Food Facts sur la source de dump alimentée par le test.</summary>
    private async Task RunImportAsync()
    {
        var (scope, job) = factory.Resolve<IOffImportJob>();

        using (scope)
            await job.RunAsync();
    }

    /// <summary>Construit une ligne JSONL au format du dump Open Food Facts.</summary>
    /// <param name="offId">Code-barres — unique, la colonne porte un index unique.</param>
    /// <param name="name">Nom du produit, sur lequel porte la recherche.</param>
    private static string DumpLine(string offId, string name) => $$$"""
        {"code":"{{{offId}}}","product_name":"{{{name}}}","allergens_tags":[],"nutriments":{"energy-kcal_100g":120,"proteins_100g":8,"carbohydrates_100g":15,"fat_100g":3}}
        """;

    /// <summary>Insère un aliment dont le nom contient le mot-clé recherché.</summary>
    private async Task<FoodItem> SeedFoodItemAsync(string keyword)
    {
        await using var context = factory.NewContext();

        var foodItem = new FoodItem(
            offId: $"{keyword}-{Guid.NewGuid()}",
            name: keyword,
            caloriesPer100g: 100f,
            proteinsPer100g: 10,
            carbsPer100g: 10,
            fatsPer100g: 10,
            allergensTags: []);

        context.FoodItems.Add(foodItem);
        await context.SaveChangesAsync();

        return foodItem;
    }
}
