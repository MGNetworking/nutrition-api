using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;


namespace NutritionApi.Application.Services;

/// <summary>
/// Implémentation de <see cref="IMealService"/>.
/// Gère la création, la consultation, la modification et la suppression des repas.
/// </summary>
public class MealService : IMealService
{
    private readonly IMealRepository _mealRepository;
    private readonly IFoodItemRepository _foodItemRepository;
    private readonly IUserRepository _userRepository;
    private readonly SubscriptionGuard _subscriptionGuard;

    public MealService(
        IMealRepository mealRepository,
        IFoodItemRepository foodItemRepository,
        IUserRepository userRepository,
        SubscriptionGuard subscriptionGuard)
    {
        _mealRepository = mealRepository;
        _foodItemRepository = foodItemRepository;
        _userRepository = userRepository;
        _subscriptionGuard = subscriptionGuard;
    }

    /// <summary>Crée un repas avec ses MealItems et calcule la NutritionInfo de chaque item.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="request">Données du repas à créer.</param>
    /// <returns>Le repas créé.</returns>
    /// <exception cref="NotFoundException">L'utilisateur ou un FoodItem référencé n'existe pas.</exception>
    /// <exception cref="ForbiddenException">La limite de repas sauvegardés est atteinte pour ce tier.</exception>
    public async Task<MealResponse> CreateAsync(Guid userId, CreateMealRequest request)
    {
        if (request.IsSaved)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
                throw new NotFoundException("User not found.");

            var savedCount = await _mealRepository.CountSavedByUserIdAsync(userId);
            _subscriptionGuard.CheckSavedMealLimit(user.SubscriptionTier, savedCount);
        }

        var mealItems = new List<MealItem>();
        var items = request.Items.Select(i => i.FoodItemId ).ToList();

        var foodItems =  await _foodItemRepository.GetByIdsAsync(items);

        if (foodItems is null)
            throw new NotFoundException("List FoodItem not found.");

        var foodItemsById = foodItems.ToDictionary(f => f.Id);


        foreach(var itemRequest in request.Items)
        {
            if (!foodItemsById.TryGetValue(itemRequest.FoodItemId, out var foodItem))
                throw new NotFoundException("FoodItem not found.");

            var nutrition = NutritionCalculator.CalculateNutrition(foodItem, itemRequest.Quantity);
            mealItems.Add(new MealItem(Guid.NewGuid(), foodItem.Id, itemRequest.Quantity, nutrition) { FoodItem = foodItem });
        }

        var meal = new Meal(
            userId,
            request.Name,
            request.MealType,
            request.Notes,
            mealItems,
            request.ConsumedAt,
            request.IsSaved);

        await _mealRepository.AddAsync(meal);
        return MealResponse.From(meal);
    }

    /// <summary>Retourne les repas de l'utilisateur avec filtres optionnels par date et statut sauvegardé.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="saved">Filtre sur le statut sauvegardé — null retourne tous les repas.</param>
    /// <param name="date">Filtre sur la date de consommation — null retourne toutes les dates.</param>
    /// <returns>Liste des repas correspondant aux filtres.</returns>
    public async Task<List<MealResponse>> GetAllAsync(Guid userId, bool? saved, DateOnly? date)
    {
        var meals = await _mealRepository.GetByUserIdAsync(userId, date, saved);
        return meals.Select(MealResponse.From).ToList();
    }

    /// <summary>Retourne le détail d'un repas appartenant à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <returns>Le repas correspondant.</returns>
    /// <exception cref="NotFoundException">Le repas n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le repas n'appartient pas à l'utilisateur.</exception>
    public async Task<MealResponse> GetByIdAsync(Guid userId, Guid mealId)
    {
        var meal = await _mealRepository.GetByIdAsync(mealId);
        if (meal is null)
            throw new NotFoundException("Meal not found.");
        if (meal.UserId != userId)
            throw new ForbiddenException("You do not have access to this meal.");
        return MealResponse.From(meal);
    }

    /// <summary>Met à jour les propriétés d'un repas appartenant à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <param name="request">Données mises à jour — seules les valeurs non nulles sont appliquées.</param>
    /// <returns>Le repas mis à jour.</returns>
    /// <exception cref="NotFoundException">Le repas n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le repas n'appartient pas à l'utilisateur.</exception>
    public async Task<MealResponse> UpdateAsync(Guid userId, Guid mealId, UpdateMealRequest request)
    {
        var meal = await _mealRepository.GetByIdAsync(mealId);
        if (meal is null)
            throw new NotFoundException("Meal not found.");
        if (meal.UserId != userId)
            throw new ForbiddenException("You do not have access to this meal.");

        if (request.Name is not null)
            meal.Rename(request.Name);
        if (request.MealType is not null)
            meal.ChangeMealType(request.MealType.Value);
        if (request.Notes is not null)
            meal.ChangeNote(request.Notes);
        if (request.ConsumedAt is not null)
            meal.ChangeConsumedAt(request.ConsumedAt.Value);

        await _mealRepository.UpdateAsync(meal);
        return MealResponse.From(meal);
    }

    /// <summary>Supprime un repas appartenant à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <exception cref="NotFoundException">Le repas n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le repas n'appartient pas à l'utilisateur.</exception>
    public async Task DeleteAsync(Guid userId, Guid mealId)
    {
        var meal = await _mealRepository.GetByIdAsync(mealId);
        if (meal is null)
            throw new NotFoundException("Meal not found.");
        if (meal.UserId != userId)
            throw new ForbiddenException("You do not have access to this meal.");

        await _mealRepository.DeleteAsync(mealId);
    }

    /// <summary>Ajoute un MealItem à un repas existant avec calcul de la NutritionInfo.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <param name="request">FoodItem et quantité à ajouter.</param>
    /// <returns>Le repas mis à jour.</returns>
    /// <exception cref="NotFoundException">Le repas ou le FoodItem n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le repas n'appartient pas à l'utilisateur.</exception>
    public async Task<MealResponse> AddItemAsync(Guid userId, Guid mealId, AddMealItemRequest request)
    {
        var meal = await _mealRepository.GetByIdAsync(mealId);
        if (meal is null)
            throw new NotFoundException("Meal not found.");
        if (meal.UserId != userId)
            throw new ForbiddenException("You do not have access to this meal.");

        var foodItem = await _foodItemRepository.GetByIdAsync(request.FoodItemId);
        if (foodItem is null)
            throw new NotFoundException("FoodItem not found.");

        var nutrition = NutritionCalculator.CalculateNutrition(foodItem, request.Quantity);
        meal.AddMealItem(new MealItem(meal.Id, foodItem.Id, request.Quantity, nutrition) { FoodItem = foodItem });

        await _mealRepository.UpdateAsync(meal);
        return MealResponse.From(meal);
    }

    /// <summary>Retire un MealItem d'un repas existant.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="mealId">Identifiant du repas.</param>
    /// <param name="itemId">Identifiant du MealItem à retirer.</param>
    /// <returns>Le repas mis à jour.</returns>
    /// <exception cref="NotFoundException">Le repas n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le repas n'appartient pas à l'utilisateur.</exception>
    public async Task<MealResponse> RemoveItemAsync(Guid userId, Guid mealId, Guid itemId)
    {
        var meal = await _mealRepository.GetByIdAsync(mealId);
        if (meal is null)
            throw new NotFoundException("Meal not found.");
        if (meal.UserId != userId)
            throw new ForbiddenException("You do not have access to this meal.");

        meal.RemoveMealItem(itemId);

        await _mealRepository.UpdateAsync(meal);
        return MealResponse.From(meal);
    }
}
