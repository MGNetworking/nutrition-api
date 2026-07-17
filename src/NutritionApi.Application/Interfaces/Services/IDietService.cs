namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Diets;

/// <summary>Contrat applicatif pour le cycle de vie des Diets.</summary>
public interface IDietService
{
    /// <summary>Lance un DietPlan et retourne la Diet active créée.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="planId">Identifiant du DietPlan à lancer.</param>
    /// <returns>La Diet active créée.</returns>
    Task<DietResponse> LaunchAsync(Guid userId, Guid planId);

    /// <summary>Retourne la Diet active de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>La Diet active correspondante.</returns>
    Task<DietResponse> GetActiveAsync(Guid userId);

    /// <summary>Retourne l'historique des Diets de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des Diets triée par date de début décroissante.</returns>
    Task<List<DietResponse>> GetHistoryAsync(Guid userId);

    /// <summary>Retourne le détail d'une Diet appartenant à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="dietId">Identifiant de la Diet.</param>
    /// <returns>La Diet correspondante.</returns>
    Task<DietResponse> GetByIdAsync(Guid userId, Guid dietId);

    /// <summary>Archive la Diet active de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="dietId">Identifiant de la Diet à archiver.</param>
    /// <returns>La Diet archivée.</returns>
    Task<DietResponse> ArchiveAsync(Guid userId, Guid dietId);
}
