namespace NutritionApi.Application.Interfaces.ExternalServices;

using NutritionApi.Application.DTOS.Admin;

/// <summary>Contrat de supervision des jobs planifiés (Hangfire).</summary>
public interface IJobMonitoringService
{
    /// <summary>Nom officiel du job d'import Open Food Facts — utilisé à l'enregistrement du job et pour le retrouver dans les statuts.</summary>
    const string ImportOffJobName = "import-off";

    /// <summary>Nom officiel du job de purge RGPD — utilisé à l'enregistrement du job et pour le retrouver dans les statuts.</summary>
    const string RgpdPurgeJobName = "rgpd-purge";

    /// <summary>Retourne le statut des jobs planifiés.</summary>
    /// <returns>Liste des jobs avec leur dernier run, prochain run et état — vide si aucun job n'est enregistré.</returns>
    Task<List<HangfireJobResponse>> GetJobsStatusAsync();
}
