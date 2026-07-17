using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.DTOS.Users;

public record UserExportResponse(
    UserProfileResponse Profile,
    List<WeightEntryResponse> WeightHistory,
    List<DietPlanResponse> DietPlans,
    List<DietResponse> Diets,
    List<MealResponse> Meals,
    List<FoodItemSearchResponse> SavedFoodItems
);
