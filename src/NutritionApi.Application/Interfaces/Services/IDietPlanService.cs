namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.DietPlans;

/// <summary>Contrat applicatif pour la gestion des DietPlans personnels et des templates partagés.</summary>
public interface IDietPlanService
{
    /// <summary>Retourne tous les DietPlans personnels de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des plans personnels de l'utilisateur.</returns>
    Task<List<DietPlanResponse>> GetUserPlansAsync(Guid userId);

    /// <summary>Crée un nouveau DietPlan personnel.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="request">Données du plan à créer.</param>
    /// <returns>Le plan créé.</returns>
    Task<DietPlanResponse> CreateAsync(Guid userId, CreateDietPlanRequest request);

    /// <summary>Met à jour les données d'un DietPlan existant appartenant à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="planId">Identifiant du plan à modifier.</param>
    /// <param name="request">Données mises à jour du plan.</param>
    /// <returns>Le plan mis à jour.</returns>
    Task<DietPlanResponse> UpdateAsync(Guid userId, Guid planId, UpdateDietPlanRequest request);

    /// <summary>Supprime un DietPlan personnel de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="planId">Identifiant du plan à supprimer.</param>
    Task DeleteAsync(Guid userId, Guid planId);

    /// <summary>Retourne les DietPlans templates partagés accessibles à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des templates disponibles.</returns>
    Task<List<DietPlanResponse>> GetTemplatesAsync(Guid userId);
}
