namespace NutritionApi.Infrastructure.Tests.Jobs;

// Tests d'intégration — nécessitent une base PostgreSQL réelle avec le schéma Hangfire.
// JobMonitoringService lit directement la table hangfire.hash (recurring-job:*), donc il
// ne peut pas être couvert par un test unitaire à mocks.
//
// Prérequis avant d'activer (banc d'essai base réelle — décision « niveau 3 docker-compose ») :
//   - Connexion à la base du docker-compose, schéma Hangfire créé au démarrage
//   - Helper pour enregistrer un recurring job de test (RecurringJob.AddOrUpdate)
//   - Nettoyage de hangfire.hash entre les tests
//
// Vérification manuelle en attendant : démarrer l'API puis appeler GET /api/v1/admin/system/health.

public class JobMonitoringServiceTest
{
    [Fact(Skip = "integration — IT-JOB-01 : aucun job récurrent enregistré → liste vide")]
    public Task GetJobsStatus_WhenNoRecurringJob_ReturnsEmpty() => Task.CompletedTask;

    [Fact(Skip = "integration — IT-JOB-02 : job enregistré jamais exécuté → 1 entrée, LastRun null, NextRun renseigné, Status Scheduled")]
    public Task GetJobsStatus_WhenRegisteredNeverRun_ReturnsScheduled() => Task.CompletedTask;

    [Fact(Skip = "integration — IT-JOB-03 : job exécuté avec succès → LastRun renseigné, Status Succeeded")]
    public Task GetJobsStatus_WhenJobSucceeded_ReturnsSucceeded() => Task.CompletedTask;

    [Fact(Skip = "integration — IT-JOB-04 : nom retourné exactement 'import-off' → garantit LastImportAt côté AdminService")]
    public Task GetJobsStatus_ReturnsExactJobName() => Task.CompletedTask;
}
