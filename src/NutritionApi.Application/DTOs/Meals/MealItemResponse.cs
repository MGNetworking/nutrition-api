namespace NutritionApi.Application.DTOS.Meals;

public record MealItemResponse(
    Guid Id,
    Guid FoodItemId,
    string FoodName,
    float Quantity,
    float Calories,
    float Proteins,
    float Carbs,
    float Fats
);
