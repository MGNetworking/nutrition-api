
namespace NutritionApi.Application.DTOS.FoodItems;

public record SavedFoodItemResponse(
    Guid Id,
    Guid FoodItemId,
    string Name,
    float CaloriesPer100g,
    DateTime SavedAt
);
