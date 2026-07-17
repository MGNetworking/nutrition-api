namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="DietPlan"/>.</summary>
public interface IDietPlanRepository
{
    /// <summary>Retourne un plan nutritionnel par son identifiant.</summary>
    /// <param name="planId">Identifiant du plan.</param>
    /// <returns>Le plan correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    Task<DietPlan?> GetByIdAsync(Guid planId);

    /// <summary>Retourne tous les plans personnels d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des plans de l'utilisateur, vide si aucun.</returns>
    Task<List<DietPlan>> GetByUserIdAsync(Guid userId);

    /// <summary>Retourne tous les plans marqués comme templates partagés.</summary>
    /// <returns>Liste des templates disponibles, vide si aucun.</returns>
    Task<List<DietPlan>> GetTemplatesAsync();

    /// <summary>Persiste un nouveau plan nutritionnel.</summary>
    /// <param name="dietPlan">Plan à ajouter.</param>
    Task AddAsync(DietPlan dietPlan);

    /// <summary>Met à jour un plan nutritionnel existant.</summary>
    /// <param name="dietPlan">Plan avec les données modifiées.</param>
    Task UpdateAsync(DietPlan dietPlan);

    /// <summary>Supprime un plan nutritionnel par son identifiant.</summary>
    /// <param name="id">Identifiant du plan à supprimer.</param>
    Task DeleteAsync(Guid id);

    /// <summary>Retourne le nombre de plans personnels d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Nombre de plans appartenant à l'utilisateur.</returns>
    Task<int> CountByUserIdAsync(Guid userId);
}
