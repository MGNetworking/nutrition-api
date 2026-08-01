namespace NutritionApi.Application.Interfaces.ExternalServices;

using NutritionApi.Application.DTOS.FoodItems;

/// <summary>Contrat de cache pour les résultats de recherche d'aliments.</summary>
public interface IFoodCacheService
{
    /// <summary>Retourne les résultats de recherche mis en cache pour un mot-clé.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <returns>Les résultats mis en cache, ou <c>null</c> si le cache est absent.</returns>
    Task<List<FoodItemSearchResponse>?> GetAsync(string keyword);

    /// <summary>Stocke les résultats de recherche en cache pour un mot-clé.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <param name="results">Résultats à mettre en cache.</param>
    Task SetAsync(string keyword, List<FoodItemSearchResponse> results);

    /// <summary>Invalide le cache pour un mot-clé donné.</summary>
    /// <param name="keyword">Mot-clé dont le cache doit être invalidé.</param>
    Task InvalidateAsync(string keyword);

    /// <summary>Invalide l'ensemble des recherches mises en cache.</summary>
    Task InvalidateAllSearchesAsync();
}
