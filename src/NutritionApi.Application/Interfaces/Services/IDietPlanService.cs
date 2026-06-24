namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.DietPlans;

/// <summary>
/// Contrat applicatif pour la gestion des DietPlans personnels.
/// Couvre le CRUD des plans personnels et la consultation des templates partagés.
/// </summary>
public interface IDietPlanService
{
    /// <summary>
    /// Retourne tous les DietPlans personnels de l'utilisateur.
    /// </summary>
    Task<List<DietPlanResponse>> GetUserPlansAsync(Guid userId);

    /// <summary>
    /// Crée un nouveau DietPlan personnel.
    /// Vérifie les limites de tier via SubscriptionGuard avant création.
    /// </summary>
    Task<DietPlanResponse> CreateAsync(Guid userId, CreateDietPlanRequest request);

    /// <summary>
    /// Met à jour les données d'un DietPlan existant appartenant à l'utilisateur.
    /// </summary>
    Task<DietPlanResponse> UpdateAsync(Guid userId, Guid planId, UpdateDietPlanRequest request);

    /// <summary>
    /// Supprime un DietPlan personnel de l'utilisateur.
    /// </summary>
    Task DeleteAsync(Guid userId, Guid planId);

    /// <summary>
    /// Retourne les DietPlans templates partagés (IsTemplate = true) accessibles à l'utilisateur.
    /// </summary>
    Task<List<DietPlanResponse>> GetTemplatesAsync(Guid userId);
}
