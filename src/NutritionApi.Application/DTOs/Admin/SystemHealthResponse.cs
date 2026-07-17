
namespace NutritionApi.Application.DTOS.Admin;

/// <summary>Statut d'un job planifié Hangfire.</summary>
/// <param name="JobName">Nom du job (voir les constantes de <c>IJobMonitoringService</c>).</param>
/// <param name="LastRun">Date de la dernière exécution (UTC), ou <c>null</c> si le job n'a jamais tourné.</param>
/// <param name="NextRun">Date de la prochaine exécution planifiée (UTC), ou <c>null</c> si non planifiée.</param>
/// <param name="Status">État du dernier run (ex : Succeeded, Failed, Scheduled).</param>
public record HangfireJobResponse(string JobName, DateTime? LastRun, DateTime? NextRun, string Status);

/// <summary>État de santé du système : jobs planifiés et catalogue d'aliments.</summary>
/// <param name="FoodItemsCount">Nombre total d'aliments dans le catalogue local.</param>
/// <param name="LastImportAt">Date du dernier import Open Food Facts (UTC), ou <c>null</c> si jamais exécuté.</param>
/// <param name="HangfireJobs">Statut de chaque job planifié.</param>
public record SystemHealthResponse(
    int FoodItemsCount,
    DateTime? LastImportAt,
    List<HangfireJobResponse> HangfireJobs
);
