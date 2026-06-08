namespace NutritionApi.Application.Services;

using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.DTOS.DietPlans;

public class RgpdService : IRgpdService
{
    private readonly IUserRepository _userRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly IDietPlanRepository _dietPlanRepository;
    private readonly IDietRepository _dietRepository;
    private readonly IMealRepository _mealRepository;
    private readonly ISavedFoodItemRepository _savedFoodItemRepository;
    private readonly IFoodItemRepository _foodItemRepository;

    public RgpdService(
        IUserRepository userRepository,
        IWeightEntryRepository weightEntryRepository,
        IDietPlanRepository dietPlanRepository,
        IDietRepository dietRepository,
        IMealRepository mealRepository,
        ISavedFoodItemRepository savedFoodItemRepository,
        IFoodItemRepository foodItemRepository)
    {
        _userRepository = userRepository;
        _weightEntryRepository = weightEntryRepository;
        _dietPlanRepository = dietPlanRepository;
        _dietRepository = dietRepository;
        _mealRepository = mealRepository;
        _savedFoodItemRepository = savedFoodItemRepository;
        _foodItemRepository = foodItemRepository;
    }

    public async Task<UserExportResponse> ExportUserDataAsync(string keycloakId)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId)
            ?? throw new NotFoundException("User profile not found.");

        var weightTask = _weightEntryRepository.GetByUserIdAsync(user.Id);
        var dietPlanTask = _dietPlanRepository.GetByUserIdAsync(user.Id);
        var dietTask = _dietRepository.GetByUserIdAsync(user.Id);
        var mealTask = _mealRepository.GetByUserIdAsync(user.Id);
        var savedFoodTask = _savedFoodItemRepository.GetByUserIdAsync(user.Id);

        await Task.WhenAll(weightTask, dietPlanTask, dietTask, mealTask, savedFoodTask);

        var weights = await weightTask;
        var dietPlans = await dietPlanTask;
        var diets = await dietTask;
        var meals = await mealTask;
        var savedFoods = await savedFoodTask;

        var foodItemIds = savedFoods.Select(sf => sf.FoodItemId).ToList();
        var foodItems = await _foodItemRepository.GetByIdsAsync(foodItemIds);

        return new UserExportResponse(
            Profile: UserProfileResponse.From(user),
            WeightHistory: weights.Select(WeightEntryResponse.From).ToList(),
            DietPlans: dietPlans.Select(DietPlanResponse.From).ToList(),
            Diets: diets.Select(DietResponse.From).ToList(),
            Meals: meals.Select(MealResponse.From).ToList(),
            SavedFoodItems: foodItems.Select(FoodItemSearchResponse.From).ToList());
    }
}
