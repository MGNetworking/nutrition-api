using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.Services;

/// <summary>
/// Service applicatif de gestion des aliments : recherche par mot-clé (cache-first Redis,
/// repli PostgreSQL) et gestion des aliments favoris (<see cref="SavedFoodItem"/>)
/// avec contrôle des limites d'abonnement.
/// </summary>
public class FoodItemService : IFoodItemService
{
    private readonly IFoodItemRepository _foodItemRepository;
    private readonly ISavedFoodItemRepository _savedFoodItemRepository;
    private readonly IFoodCacheService _foodCache;
    private readonly IUserRepository _userRepository;
    private readonly SubscriptionGuard _subscriptionGuard;

    public FoodItemService(
        IFoodItemRepository foodItemRepository,
        ISavedFoodItemRepository savedFoodItemRepository,
        IFoodCacheService foodCache,
        IUserRepository userRepository,
        SubscriptionGuard subscriptionGuard)
    {
        _foodItemRepository = foodItemRepository;
        _savedFoodItemRepository = savedFoodItemRepository;
        _foodCache = foodCache;
        _userRepository = userRepository;
        _subscriptionGuard = subscriptionGuard;
    }



    /// <summary>
    /// Liste les aliments favoris d'un utilisateur, enrichis des informations
    /// nutritionnelles de l'aliment référencé. Les favoris dont l'aliment
    /// n'existe plus dans le catalogue sont ignorés.
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des favoris, vide si l'utilisateur n'en a aucun.</returns>
    public async Task<List<SavedFoodItemResponse>> GetSavedAsync(Guid userId)
    {
        var foodItemSaved = await _savedFoodItemRepository.GetByUserIdAsync(userId);
        var foodsId = foodItemSaved.Select(i => i.FoodItemId).ToList();

        var foodItems = await _foodItemRepository.GetByIdsAsync(foodsId);
        var foodItemsById = foodItems.ToDictionary(i => i.Id);

        var foodItemsReponses = new List<SavedFoodItemResponse>();

        foreach (var saved in foodItemSaved)
        {
            if (foodItemsById.TryGetValue(saved.FoodItemId, out var foodItem))
                foodItemsReponses.Add(SavedFoodItemResponse.From(saved, foodItem));
        }


        return foodItemsReponses;
    }

    /// <summary>
    /// Supprime un aliment favori après vérification de son existence
    /// et de son appartenance à l'utilisateur.
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="savedId">Identifiant de l'entrée favori à supprimer.</param>
    /// <exception cref="NotFoundException">Le favori n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le favori n'appartient pas à l'utilisateur.</exception>
    public async Task RemoveSavedAsync(Guid userId, Guid savedId)
    {
        var saveFoodItem = await _savedFoodItemRepository.GetByIdAsync(savedId);

        if (saveFoodItem is null)
            throw new NotFoundException("Saved food item not found.");

        if (saveFoodItem.UserId != userId)
            throw new ForbiddenException("You are not authorized to remove this saved food item.");

        await _savedFoodItemRepository.DeleteAsync(savedId);

    }

    /// <summary>
    /// Sauvegarde un aliment en favori pour l'utilisateur, après contrôle
    /// du doublon et de la limite de favoris du tier d'abonnement
    /// (Free : 10, Pro : 100, Business : illimité).
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="request">Données de l'aliment à sauvegarder.</param>
    /// <returns>Le favori créé, enrichi des informations nutritionnelles de l'aliment.</returns>
    /// <exception cref="NotFoundException">L'utilisateur n'existe pas.</exception>
    /// <exception cref="NotFoundException">L'aliment demandé n'existe pas.</exception>
    /// <exception cref="ConflictException">L'aliment est déjà dans les favoris de l'utilisateur.</exception>
    /// <exception cref="ForbiddenException">La limite de favoris du tier est atteinte.</exception>
    public async Task<SavedFoodItemResponse> SaveAsync(Guid userId, SaveFoodItemRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User not found.");

        // Check doublon
        var savedFoodItem = await _savedFoodItemRepository.GetByUserIdAndFoodItemIdAsync(userId, request.FoodItemId);
        if (savedFoodItem is not null)
            throw new ConflictException("You have already saved this food item.");

        // Check limit favoris
        var savedCount = await _savedFoodItemRepository.CountByUserIdAsync(userId);
        _subscriptionGuard.CheckSavedFoodItemLimit(user.SubscriptionTier, savedCount);

        var foodItem = await _foodItemRepository.GetByIdAsync(request.FoodItemId);
        if (foodItem is null)
            throw new NotFoundException("The requested food item was not found.");

        savedFoodItem = new SavedFoodItem(userId, foodItem.Id);

        await _savedFoodItemRepository.AddAsync(savedFoodItem);
        return SavedFoodItemResponse.From(savedFoodItem, foodItem);
    }

    /// <summary>
    /// Recherche des aliments par mot-clé, en interrogeant le cache en premier.
    /// Sur cache miss, la recherche est effectuée en base et le résultat complet
    /// est mis en cache avant d'être tronqué à <paramref name="limit"/>.
    /// </summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <param name="limit">Nombre maximal de résultats retournés.</param>
    /// <returns>Liste des aliments correspondants, vide si aucun résultat.</returns>
    public async Task<List<FoodItemSearchResponse>> SearchAsync(string keyword, int limit = 20)
    {
        var cachedItems = await _foodCache.GetAsync(keyword);
        if (cachedItems is not null)
            return cachedItems.Take(limit).ToList();

        var foodItems = await _foodItemRepository.SearchByKeywordAsync(keyword, 20);
        var foodItemsResponses = foodItems.Select(FoodItemSearchResponse.From).ToList();

        await _foodCache.SetAsync(keyword, foodItemsResponses);

        return foodItemsResponses.Take(limit).ToList();
    }
}
