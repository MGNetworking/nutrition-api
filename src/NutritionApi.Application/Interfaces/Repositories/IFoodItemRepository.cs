namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="FoodItem"/>.</summary>
public interface IFoodItemRepository
{
    /// <summary>Retourne un aliment par son identifiant.</summary>
    /// <param name="id">Identifiant de l'aliment.</param>
    /// <returns>L'aliment correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    Task<FoodItem?> GetByIdAsync(Guid id);

    /// <summary>Retourne une liste d'aliments par leurs identifiants.</summary>
    /// <param name="ids">Liste des identifiants d'aliments.</param>
    /// <returns>Liste des aliments correspondants.</returns>
    Task<List<FoodItem>> GetByIdsAsync(List<Guid> ids);

    /// <summary>Retourne un aliment par son identifiant Open Food Facts.</summary>
    /// <param name="offId">Identifiant Open Food Facts de l'aliment.</param>
    /// <returns>L'aliment correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    Task<FoodItem?> GetByOffIdAsync(string offId);

    /// <summary>Recherche des aliments par mot-clé dans le catalogue.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <param name="limit">Nombre maximum de résultats.</param>
    /// <returns>Liste des aliments correspondants.</returns>
    Task<List<FoodItem>> SearchByKeywordAsync(string keyword, int limit = 20);

    /// <summary>Persiste un nouvel aliment.</summary>
    /// <param name="foodItem">Aliment à ajouter.</param>
    Task AddAsync(FoodItem foodItem);

    /// <summary>Met à jour un aliment existant.</summary>
    /// <param name="foodItem">Aliment avec les données modifiées.</param>
    Task UpdateAsync(FoodItem foodItem);

    /// <summary>Retourne le nombre total d'aliments dans le catalogue.</summary>
    /// <returns>Nombre d'aliments.</returns>
    Task<int> CountAsync();
}
