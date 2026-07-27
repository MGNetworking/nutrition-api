namespace NutritionApi.Infrastructure.Jobs.RgpdPurge;

/// <summary>Job de purge des comptes dont la demande de suppression RGPD est arrivée à échéance.</summary>
public interface IRgpdPurgeJob
{
    /// <summary>Supprime définitivement les comptes dont la grace period est expirée.</summary>
    Task RunAsync();
}
