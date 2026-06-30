namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Diets;

/// <summary>
/// Contrat applicatif pour le cycle de vie des Diets.
/// Couvre le lancement d'un DietPlan vers une Diet active, l'archivage et la consultation.
/// </summary>
public interface IDietService
{
    /// <summary>
    /// Lance un DietPlan et retourne la Diet active créée.
    /// </summary>
    Task<DietResponse> LaunchAsync(Guid userId, Guid planId);

    /// <summary>
    /// Retourne la Diet active de l'utilisateur.
    /// </summary>
    Task<DietResponse> GetActiveAsync(Guid userId);

    /// <summary>
    /// Retourne l'historique des Diets de l'utilisateur.
    /// </summary>
    Task<List<DietResponse>> GetHistoryAsync(Guid userId);

    /// <summary>
    /// Retourne le détail d'une Diet appartenant à l'utilisateur.
    /// </summary>
    Task<DietResponse> GetByIdAsync(Guid userId, Guid dietId);

    /// <summary>
    /// Archive la Diet active de l'utilisateur.
    /// </summary>
    Task<DietResponse> ArchiveAsync(Guid userId, Guid dietId);
}
