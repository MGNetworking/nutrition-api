namespace NutritionApi.Infrastructure.Scheduling;

using Hangfire.Server;
using NutritionApi.Infrastructure.Observability;
using System.Diagnostics;

/// <summary>
/// Mesure la durée et l'issue de chaque job Hangfire (NTR-138).
/// </summary>
/// <remarks>
/// Un filtre serveur plutôt qu'une mesure écrite dans chaque job : l'import Open Food Facts, la
/// purge RGPD et tout job ajouté plus tard sont couverts sans y toucher. Hangfire ne publie aucune
/// métrique de lui-même — <c>JobMonitoringService</c> lit bien l'état des jobs récurrents, mais
/// seulement à la demande, sur appel de la route d'administration.
/// </remarks>
public sealed class JobMetricsFilter : IServerFilter
{
    /// <summary>Clé sous laquelle le chronomètre voyage d'un point de mesure à l'autre.</summary>
    private const string CleDuChronometre = "nutrition.job.stopwatch";

    private readonly InfrastructureMetrics _metriques;

    /// <summary>Construit le filtre.</summary>
    /// <param name="metriques">Compteurs de la couche Infrastructure.</param>
    public JobMetricsFilter(InfrastructureMetrics metriques) => _metriques = metriques;

    /// <summary>Démarre le chronomètre avant l'exécution du job.</summary>
    /// <param name="context">Contexte d'exécution fourni par Hangfire.</param>
    public void OnPerforming(PerformingContext context) =>
        context.Items[CleDuChronometre] = Stopwatch.StartNew();

    /// <summary>Enregistre la durée et l'issue une fois le job achevé.</summary>
    /// <param name="context">
    /// Contexte d'exécution. Sa propriété <c>Exception</c> porte l'échec éventuel : le job est
    /// compté comme échoué sans que le filtre ait à l'intercepter ni à le masquer.
    /// </param>
    public void OnPerformed(PerformedContext context)
    {
        if (context.Items.TryGetValue(CleDuChronometre, out var valeur) && valeur is Stopwatch chronometre)
        {
            chronometre.Stop();

            _metriques.JobExecute(
                job: context.BackgroundJob.Job.Type.Name,
                issue: context.Exception is null ? "success" : "failure",
                duree: chronometre.Elapsed);
        }
    }
}
