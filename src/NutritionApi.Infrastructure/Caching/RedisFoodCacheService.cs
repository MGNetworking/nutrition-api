using Microsoft.Extensions.Configuration;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Interfaces.ExternalServices;
using StackExchange.Redis;
using System.Text.Json;

namespace NutritionApi.Infrastructure.Caching;

/// <summary>Cache Redis des résultats de recherche d'aliments.</summary>
/// <remarks>
/// Les entrées expirent d'elles-mêmes au terme de leur durée de vie : aucune invalidation
/// périodique n'est nécessaire. <see cref="InvalidateAsync"/> ne sert qu'aux suppressions ciblées.
/// </remarks>
public sealed class RedisFoodCacheService : IFoodCacheService
{
    /// <summary>Durée de vie par défaut si la configuration ne la précise pas — alignée sur la fréquence du job d'import.</summary>
    private const int DefaultTtlHours = 24;

    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _ttl;

    public RedisFoodCacheService(IConnectionMultiplexer redis, IConfiguration configuration)
    {
        _redis = redis;
        _ttl = TimeSpan.FromHours(configuration.GetValue("Redis:SearchCacheTtlHours", DefaultTtlHours));
    }

    /// <summary>Construit la clé Redis d'un mot-clé — normalisé en minuscules et sans espaces de bord.</summary>
    private static string BuildKey(string keyword)
        => $"food:search:{keyword.ToLowerInvariant().Trim()}";

    /// <summary>Retourne les résultats mis en cache pour un mot-clé.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <returns>Les résultats en cache, ou <c>null</c> si la clé est absente ou expirée.</returns>
    public async Task<List<FoodItemSearchResponse>?> GetAsync(string keyword)
    {
        var cached = await _redis.GetDatabase().StringGetAsync(BuildKey(keyword));

        return cached.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<List<FoodItemSearchResponse>>(cached.ToString());
    }

    /// <summary>Met les résultats en cache pour un mot-clé, avec la durée de vie configurée.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <param name="results">Résultats à mettre en cache.</param>
    public async Task SetAsync(string keyword, List<FoodItemSearchResponse> results)
    {
        var payload = JsonSerializer.Serialize(results);
        await _redis.GetDatabase().StringSetAsync(BuildKey(keyword), payload, _ttl);
    }

    /// <summary>Supprime l'entrée de cache d'un mot-clé — sans effet si elle n'existe pas.</summary>
    /// <param name="keyword">Mot-clé dont le cache doit être supprimé.</param>
    public async Task InvalidateAsync(string keyword)
        => await _redis.GetDatabase().KeyDeleteAsync(BuildKey(keyword));
}
