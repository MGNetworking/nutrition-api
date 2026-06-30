namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.FoodItems;

/// <summary>Contrat applicatif pour la recherche d'aliments et la gestion des favoris.</summary>
public interface IFoodItemService
{
    /// <summary>Recherche des aliments par mot-clé.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <param name="limit">Nombre maximum de résultats retournés.</param>
    /// <returns>Liste des aliments correspondant au mot-clé.</returns>
    Task<List<FoodItemSearchResponse>> SearchAsync(string keyword, int limit = 20);

    /// <summary>Retourne les aliments sauvegardés par un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des aliments favoris de l'utilisateur.</returns>
    Task<List<SavedFoodItemResponse>> GetSavedAsync(Guid userId);

    /// <summary>Sauvegarde un aliment dans les favoris de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="request">Identifiant de l'aliment à sauvegarder.</param>
    /// <returns>L'aliment favori créé.</returns>
    Task<SavedFoodItemResponse> SaveAsync(Guid userId, SaveFoodItemRequest request);

    /// <summary>Retire un aliment des favoris de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="savedId">Identifiant de l'entrée favori à supprimer.</param>
    Task RemoveSavedAsync(Guid userId, Guid savedId);
}
