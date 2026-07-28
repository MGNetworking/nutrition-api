namespace NutritionApi.Infrastructure.Scheduling;

using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Infrastructure.Jobs.RgpdPurge;

/// <summary>
/// Déclare les jobs récurrents de l'application dans le storage Hangfire au démarrage.
/// </summary>
/// <remarks>
/// Ce service ne fait qu'écrire des définitions — identifiant, méthode cible, expression cron.
/// Il n'exécute aucun job : c'est le rôle du serveur Hangfire, qui lit ces définitions.
/// L'appel est idempotent, il peut donc être rejoué à chaque démarrage sans créer de doublon.
/// </remarks>
public sealed class RecurringJobRegistrationService : IHostedService
{
    /// <summary>Import Open Food Facts — chaque nuit à 03h00 UTC.</summary>
    private const string ImportOffCron = "0 3 * * *";

    /// <summary>Purge RGPD — chaque nuit à 03h30 UTC, décalée pour ne pas concurrencer l'import.</summary>
    private const string RgpdPurgeCron = "30 3 * * *";

    private readonly IRecurringJobManager _recurringJobs;
    private readonly ILogger<RecurringJobRegistrationService> _logger;

    /// <summary>Construit le service d'enregistrement.</summary>
    /// <param name="recurringJobs">Gestionnaire des jobs récurrents Hangfire.</param>
    /// <param name="logger">Journal des échecs d'enregistrement.</param>
    public RecurringJobRegistrationService(
        IRecurringJobManager recurringJobs,
        ILogger<RecurringJobRegistrationService> logger)
    {
        _recurringJobs = recurringJobs;
        _logger = logger;
    }

    /// <summary>
    /// Inscrit les jobs récurrents dans le storage. Un storage indisponible est journalisé sans
    /// interrompre le démarrage : le trafic HTTP ne dépend pas de Hangfire, et faire échouer
    /// l'application entière parce qu'un planificateur est injoignable serait disproportionné.
    /// Les jobs restent alors non planifiés — l'absence se constate sur <c>GET /admin/system/health</c>.
    /// </summary>
    /// <param name="cancellationToken">Jeton d'annulation du démarrage.</param>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _recurringJobs.AddOrUpdate<IOffImportJob>(
                IJobMonitoringService.ImportOffJobName,
                job => job.RunAsync(),
                ImportOffCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            _recurringJobs.AddOrUpdate<IRgpdPurgeJob>(
                IJobMonitoringService.RgpdPurgeJobName,
                job => job.RunAsync(),
                RgpdPurgeCron,
                new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

            _logger.LogInformation("Jobs récurrents enregistrés : {ImportOff}, {RgpdPurge}.",
                IJobMonitoringService.ImportOffJobName,
                IJobMonitoringService.RgpdPurgeJobName);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Enregistrement des jobs récurrents impossible — ils ne seront pas planifiés. L'API démarre malgré tout.");
        }

        return Task.CompletedTask;
    }

    /// <summary>Ne fait rien : les définitions vivent dans le storage, pas dans le processus.</summary>
    /// <param name="cancellationToken">Jeton d'annulation de l'arrêt.</param>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
