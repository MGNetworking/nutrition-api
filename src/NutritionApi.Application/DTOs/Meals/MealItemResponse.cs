
namespace NutritionApi.Application.DTOS.Meals;

using NutritionApi.Domain.Entity;

public record MealItemResponse(
    Guid Id,
    Guid FoodItemId,
    string FoodName,
    float Quantity,
    float Calories,
    float Proteins,
    float Carbs,
    float Fats
)
{
    public static MealItemResponse From(MealItem mealItem)
        => new(mealItem.Id,
            mealItem.FoodItemId,
            mealItem.FoodItem!.Name,
            mealItem.Quantity,
            mealItem.Nutrition.Calories,
            mealItem.Nutrition.Proteins,
            mealItem.Nutrition.Carbs,
            mealItem.Nutrition.Fats);
}
