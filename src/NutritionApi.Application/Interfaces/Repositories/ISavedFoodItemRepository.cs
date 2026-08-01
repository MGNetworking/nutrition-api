namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="SavedFoodItem"/>.</summary>
public interface ISavedFoodItemRepository
{
    /// <summary>Retourne un aliment favori par son identifiant.</summary>
    /// <param name="id">Identifiant de l'entrée favori.</param>
    /// <returns>L'entrée correspondante, ou <c>null</c> si elle n'existe pas.</returns>
    Task<SavedFoodItem?> GetByIdAsync(Guid id);

    /// <summary>Retourne un aliment favori d'un utilisateur par l'identifiant de l'aliment.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="foodItemId">Identifiant de l'aliment.</param>
    /// <returns>L'entrée correspondante, ou <c>null</c> si elle n'existe pas.</returns>
    Task<SavedFoodItem?> GetByUserIdAndFoodItemIdAsync(Guid userId, Guid foodItemId);

    /// <summary>Retourne tous les aliments favoris d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des aliments favoris, vide si aucun.</returns>
    Task<List<SavedFoodItem>> GetByUserIdAsync(Guid userId);

    /// <summary>Persiste un nouvel aliment favori.</summary>
    /// <param name="savedFoodItem">Aliment favori à ajouter.</param>
    Task AddAsync(SavedFoodItem savedFoodItem);

    /// <summary>Supprime un aliment favori par son identifiant.</summary>
    /// <param name="id">Identifiant de l'entrée favori à supprimer.</param>
    Task DeleteAsync(Guid id);

    /// <summary>Retourne le nombre d'aliments favoris d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Nombre d'aliments favoris.</returns>
    Task<int> CountByUserIdAsync(Guid userId);
}
