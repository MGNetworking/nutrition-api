namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Meals;

/// <summary>Contrat applicatif pour la gestion des repas.</summary>
public interface IMealService
{
    /// <summary>Crée un nouveau repas pour l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="request">Données du repas à créer.</param>
    /// <returns>Le repas créé.</returns>
    Task<MealResponse> CreateAsync(Guid userId, CreateMealRequest request);

    /// <summary>Retourne les repas d'un utilisateur avec filtrage optionnel.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="saved">Filtre sur les repas sauvegardés uniquement.</param>
    /// <param name="date">Filtre sur une date de consommation spécifique.</param>
    /// <returns>Liste des repas correspondant aux critères.</returns>
    Task<List<MealResponse>> GetAllAsync(Guid userId, bool? saved, DateOnly? date);

    /// <summary>Retourne le détail d'un repas appartenant à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <returns>Le repas correspondant.</returns>
    Task<MealResponse> GetByIdAsync(Guid userId, Guid mealId);

    /// <summary>Met à jour un repas existant.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas à modifier.</param>
    /// <param name="request">Données mises à jour du repas.</param>
    /// <returns>Le repas mis à jour.</returns>
    Task<MealResponse> UpdateAsync(Guid userId, Guid mealId, UpdateMealRequest request);

    /// <summary>Supprime un repas de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas à supprimer.</param>
    Task DeleteAsync(Guid userId, Guid mealId);

    /// <summary>Ajoute un aliment à un repas.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <param name="request">Données de l'aliment à ajouter.</param>
    /// <returns>Le repas mis à jour avec le nouvel aliment.</returns>
    Task<MealResponse> AddItemAsync(Guid userId, Guid mealId, AddMealItemRequest request);

    /// <summary>Retire un aliment d'un repas.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <param name="itemId">Identifiant de l'aliment à retirer.</param>
    /// <returns>Le repas mis à jour sans l'aliment retiré.</returns>
    Task<MealResponse> RemoveItemAsync(Guid userId, Guid mealId, Guid itemId);
}
