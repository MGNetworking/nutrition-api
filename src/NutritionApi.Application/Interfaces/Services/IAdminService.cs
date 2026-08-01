namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;

/// <summary>Contrat applicatif pour les opérations d'administration.</summary>
public interface IAdminService
{
    /// <summary>Retourne les KPIs consolidés du tableau de bord administrateur.</summary>
    /// <returns>Les indicateurs clés de performance.</returns>
    Task<AdminDashboardResponse> GetDashboardAsync();

    /// <summary>Retourne l'état de santé des jobs Hangfire et du catalogue d'aliments.</summary>
    /// <returns>Le statut système courant.</returns>
    Task<SystemHealthResponse> GetSystemHealthAsync();

    /// <summary>Crée un plan nutritionnel template mis à disposition des utilisateurs.</summary>
    /// <param name="request">Données du template à créer.</param>
    /// <returns>Le plan template créé.</returns>
    Task<DietPlanResponse> CreateTemplateAsync(CreateDietPlanRequest request);

    /// <summary>Met à jour un plan nutritionnel template existant.</summary>
    /// <param name="templateId">Identifiant du template à modifier.</param>
    /// <param name="request">Données mises à jour du template.</param>
    /// <returns>Le plan template mis à jour.</returns>
    Task<DietPlanResponse> UpdateTemplateAsync(Guid templateId, UpdateDietPlanRequest request);

    /// <summary>Supprime un plan nutritionnel template.</summary>
    /// <param name="templateId">Identifiant du template à supprimer.</param>
    Task DeleteTemplateAsync(Guid templateId);
}
