using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Infrastructure.Observability;
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

    /// <summary>Préfixe commun des clés de recherche — sert aussi de motif d'invalidation globale.</summary>
    private const string KeyPrefix = "food:search:";

    /// <summary>
    /// Version de la forme sérialisée des entrées. À incrémenter dès que <see cref="FoodItemSearchResponse"/>
    /// change de forme : les entrées de l'ancienne version deviennent inatteignables et expirent seules,
    /// au lieu de provoquer des erreurs de désérialisation jusqu'au terme de leur durée de vie.
    /// </summary>
    private const string SchemaVersion = "v1";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisFoodCacheService> _logger;
    private readonly InfrastructureMetrics _metriques;
    private readonly TimeSpan _ttl;

    public RedisFoodCacheService(
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<RedisFoodCacheService> logger,
        InfrastructureMetrics metriques)
    {
        _redis = redis;
        _logger = logger;
        _metriques = metriques;
        _ttl = TimeSpan.FromHours(configuration.GetValue("Redis:SearchCacheTtlHours", DefaultTtlHours));
    }

    /// <summary>Construit la clé Redis d'un mot-clé — normalisé en minuscules et sans espaces de bord.</summary>
    private static string BuildKey(string keyword)
        => $"{KeyPrefix}{SchemaVersion}:{keyword.ToLowerInvariant().Trim()}";

    /// <summary>Retourne les résultats mis en cache pour un mot-clé.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <returns>
    /// Les résultats en cache, ou <c>null</c> si la clé est absente, expirée, ou si Redis est
    /// indisponible — l'appelant traite ces trois cas de la même façon : il interroge la base.
    /// </returns>
    /// <exception cref="JsonException">
    /// L'entrée en cache n'a pas la forme attendue. Volontairement non interceptée : c'est un défaut
    /// de code, pas une panne d'infrastructure (voir <see cref="SchemaVersion"/>).
    /// </exception>
    public async Task<List<FoodItemSearchResponse>?> GetAsync(string keyword)
    {
        RedisValue cached;

        try
        {
            cached = await _redis.GetDatabase().StringGetAsync(BuildKey(keyword));
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache indisponible en lecture — repli sur la base de données");
            _metriques.CacheFailure();
            return null;
        }

        if (cached.IsNullOrEmpty)
        {
            _metriques.CacheMiss();
            return null;
        }

        _metriques.CacheHit();

        return JsonSerializer.Deserialize<List<FoodItemSearchResponse>>(cached.ToString());
    }

    /// <summary>
    /// Met les résultats en cache pour un mot-clé, avec la durée de vie configurée. Une indisponibilité
    /// de Redis est journalisée puis ignorée : le résultat a déjà été retourné à l'appelant.
    /// </summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <param name="results">Résultats à mettre en cache.</param>
    public async Task SetAsync(string keyword, List<FoodItemSearchResponse> results)
    {
        try
        {
            var payload = JsonSerializer.Serialize(results);
            await _redis.GetDatabase().StringSetAsync(BuildKey(keyword), payload, _ttl);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Cache indisponible en écriture — résultat non mis en cache");
        }
    }

    /// <summary>Supprime l'entrée de cache d'un mot-clé — sans effet si elle n'existe pas.</summary>
    /// <param name="keyword">Mot-clé dont le cache doit être supprimé.</param>
    public async Task InvalidateAsync(string keyword)
        => await _redis.GetDatabase().KeyDeleteAsync(BuildKey(keyword));

    /// <summary>
    /// Supprime toutes les recherches en cache — appelé après un import qui a modifié le
    /// catalogue, les résultats mémorisés portant alors sur des données périmées.
    /// </summary>
    /// <remarks>
    /// Le parcours des clés par motif n'est possible qu'avec <c>IConnectionMultiplexer</c> :
    /// les mots-clés naissent des saisies utilisateur et ne sont recensés nulle part.
    /// <para>
    /// Le motif ignore <see cref="SchemaVersion"/> : les entrées écrites par une version antérieure
    /// sont supprimées elles aussi.
    /// </para>
    /// <para>
    /// Une panne Redis est interceptée, comme dans <see cref="GetAsync"/> et <see cref="SetAsync"/>.
    /// La laisser remonter faisait échouer l'import entier pour un nettoyage de cache raté : le
    /// planificateur retéléchargeait alors le dump et retraitait plusieurs millions de lignes, alors
    /// que les aliments étaient déjà en base et que seul le nettoyage manquait. Disproportionné.
    /// </para>
    /// <para>
    /// Ce que coûte l'interception : les recherches mémorisées portent sur l'ancien catalogue
    /// jusqu'à l'expiration de leur durée de vie, ou jusqu'au prochain import qui réussira son
    /// nettoyage. Une panne Redis relève de l'exploitation du système, pas de l'import.
    /// </para>
    /// </remarks>
    public async Task InvalidateAllSearchesAsync()
    {
        try
        {
            var db = _redis.GetDatabase();

            foreach (var endpoint in _redis.GetEndPoints())
            {
                var server = _redis.GetServer(endpoint);

                foreach (var key in server.Keys(pattern: $"{KeyPrefix}*"))
                    await db.KeyDeleteAsync(key);
            }
        }
        // RedisTimeoutException dérive de TimeoutException, pas de RedisException : un catch sur
        // cette dernière seule laisse passer le cas réel. Le parcours des clés attend la réponse au
        // SCAN et expire, là où une lecture simple échoue d'abord sur l'absence de connexion.
        catch (Exception ex) when (ex is RedisException or RedisTimeoutException)
        {
            _logger.LogWarning(
                ex,
                "Cache indisponible au nettoyage — les recherches mémorisées restent sur l'ancien "
                + "catalogue jusqu'à l'expiration de leur durée de vie ({Heures} h).",
                _ttl.TotalHours);
        }
    }
}
