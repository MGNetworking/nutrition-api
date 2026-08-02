namespace NutritionApi.Api.HealthChecks;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

/// <summary>
/// Vérifie que l'instance dispose de clés de signature utilisables pour valider les jetons.
/// </summary>
/// <remarks>
/// La question posée est « ai-je des clés ? », et non « le serveur d'identité répond-il ? ». La
/// distinction n'est pas cosmétique : la validation des jetons est locale, à partir des clés
/// publiques du realm mises en cache. Une instance qui les a obtenues continue de servir
/// normalement pendant que le serveur d'identité est à terre.
/// <para>
/// Sonder la joignabilité de Keycloak reviendrait à retirer du service une flotte entière — toutes
/// ses instances répondant correctement — le jour d'un redémarrage du serveur d'identité. La panne
/// serait causée par la sonde censée l'éviter. Décision actée le 2026-08-02 (NTR-172).
/// </para>
/// <para>
/// Les deux cas se répartissent ainsi :
/// <list type="bullet">
///   <item>instance ancienne, clés en cache, Keycloak absent — <b>saine</b>, elle continue de servir ;</item>
///   <item>instance neuve, aucune clé, Keycloak absent — <b>défaillante</b>, elle reste hors du service
///   jusqu'à ce qu'elle en obtienne.</item>
/// </list>
/// </para>
/// <para>
/// Aucune boucle d'attente ici : <c>ConfigurationManager</c> récupère les clés paresseusement et
/// réessaie de lui-même. Quand une configuration valide est déjà connue, un échec de rafraîchissement
/// la laisse en place plutôt que de la vider — c'est ce qui rend le premier cas possible.
/// </para>
/// </remarks>
public sealed class SigningKeysHealthCheck : IHealthCheck
{
    /// <summary>Nom sous lequel la sonde est enregistrée et publiée.</summary>
    public const string Name = "cles-de-signature";

    /// <summary>
    /// Délai au-delà duquel la sonde renonce. Une sonde qui pend est pire qu'une sonde qui échoue :
    /// l'orchestrateur attendrait son propre délai d'expiration sans rien apprendre.
    /// </summary>
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptions;

    /// <summary>Construit la sonde.</summary>
    /// <param name="jwtOptions">Options du schéma JWT — portent le gestionnaire de configuration OIDC.</param>
    public SigningKeysHealthCheck(IOptionsMonitor<JwtBearerOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var options = _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);

        if (options.ConfigurationManager is null)
        {
            return HealthCheckResult.Unhealthy(
                "Aucun gestionnaire de configuration OIDC : la clé Keycloak:Authority est probablement absente.");
        }

        using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limite.CancelAfter(Timeout);

        try
        {
            var configuration = await options.ConfigurationManager.GetConfigurationAsync(limite.Token);

            if (configuration.SigningKeys.Count == 0)
            {
                return HealthCheckResult.Unhealthy(
                    $"Le serveur d'identité « {options.Authority} » n'a publié aucune clé de signature. "
                    + "Aucun jeton ne peut être validé.");
            }

            return HealthCheckResult.Healthy(
                $"{configuration.SigningKeys.Count} clé(s) de signature disponible(s).");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"Les clés de signature n'ont pas pu être obtenues en {Timeout.TotalSeconds:0} s "
                + $"depuis « {options.Authority} ».");
        }
        catch (Exception exception)
        {
            // Ce cas ne se produit que si aucune configuration valide n'a jamais été obtenue : une
            // fois le cache peuplé, un échec de rafraîchissement ne lève pas.
            return HealthCheckResult.Unhealthy(
                $"Aucune clé de signature disponible, et « {options.Authority} » est injoignable.",
                exception);
        }
    }
}
