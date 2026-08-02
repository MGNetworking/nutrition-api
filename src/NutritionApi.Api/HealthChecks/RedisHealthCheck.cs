namespace NutritionApi.Api.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

/// <summary>
/// Signale l'état du cache Redis, sans jamais rendre l'instance inapte.
/// </summary>
/// <remarks>
/// Redis est un accélérateur, pas une dépendance fonctionnelle : la recherche d'aliments retombe sur
/// PostgreSQL quand le cache est absent. Plus lente, mais correcte. Retirer l'instance du service
/// pour autant transformerait une dégradation en panne.
/// <para>
/// La sonde renvoie donc <see cref="HealthStatus.Degraded"/> et non
/// <see cref="HealthStatus.Unhealthy"/> : le statut dégradé est publié dans la réponse, visible, mais
/// laisse le point de terminaison répondre 200.
/// </para>
/// <para>
/// Ce que cela ferme, constaté en fermant NTR-150 : Redis pouvait être mort depuis trois jours, les
/// recherches plus lentes, des avertissements empilés dans des journaux que personne ne lit — et le
/// tableau de bord affichait que tout allait bien.
/// </para>
/// <para>
/// Écrite à la main plutôt qu'empruntée à AspNetCore.HealthChecks.Redis : ce paquet plafonne en
/// 9.0.0, compilé contre StackExchange.Redis 2.x, quand le projet est en 3.0.17. NuGet unifierait
/// vers la version du projet, mais le paquet aurait été compilé contre la précédente.
/// </para>
/// </remarks>
public sealed class RedisHealthCheck : IHealthCheck
{
    /// <summary>Nom sous lequel la sonde est enregistrée et publiée.</summary>
    public const string Name = "redis";

    /// <summary>Délai au-delà duquel la sonde renonce et déclare le cache dégradé.</summary>
    public static readonly TimeSpan DelaiParDefaut = TimeSpan.FromSeconds(3);

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisHealthCheck> _logger;
    private readonly TimeSpan _delai;

    /// <summary>Construit la sonde.</summary>
    /// <param name="redis">Multiplexeur partagé, enregistré en singleton par la couche Infrastructure.</param>
    /// <param name="logger">
    /// Journaliseur. Chaque dégradation y laisse une trace (NTR-137) : l'orchestrateur n'enregistre
    /// que le code HTTP, jamais la cause, et ses événements expirent en une heure. Le niveau est
    /// <c>Warning</c> et non <c>Error</c> — le service continue de répondre, en repli sur PostgreSQL.
    /// </param>
    /// <param name="delai">
    /// Délai avant renoncement. Laissé vide en production ; un test qui éprouve cette branche en
    /// passe un court, faute de quoi chaque exécution attendrait trois secondes pour rien.
    /// </param>
    public RedisHealthCheck(
        IConnectionMultiplexer redis,
        ILogger<RedisHealthCheck> logger,
        TimeSpan? delai = null)
    {
        _redis = redis;
        _logger = logger;
        _delai = delai ?? DelaiParDefaut;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_redis.IsConnected)
        {
            _logger.LogWarning(
                "Sonde Redis : cache injoignable — les recherches d'aliments retombent sur PostgreSQL.");

            return HealthCheckResult.Degraded(
                "Cache Redis injoignable — les recherches d'aliments retombent sur PostgreSQL.");
        }

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(_delai);

        try
        {
            var latence = await _redis.GetDatabase().PingAsync().WaitAsync(limite.Token);

            return HealthCheckResult.Healthy($"Cache Redis joignable — {latence.TotalMilliseconds:0} ms.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Sonde Redis : aucune réponse en {Delai} s — repli sur PostgreSQL.",
                _delai.TotalSeconds);

            return HealthCheckResult.Degraded(
                $"Cache Redis sans réponse en {_delai.TotalSeconds:0} s — repli sur PostgreSQL.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Sonde Redis : cache en erreur — les recherches d'aliments retombent sur PostgreSQL.");

            return HealthCheckResult.Degraded(
                "Cache Redis en erreur — les recherches d'aliments retombent sur PostgreSQL.",
                exception);
        }
    }
}
