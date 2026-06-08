using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.Meals;

public record MealResponse(
    Guid Id,
    string Name,
    MealType MealType,
    DateTime ConsumedAt,
    string? Notes,
    bool IsSaved,
    List<MealItemResponse> Items
)
{
    public static MealResponse From(Meal meal)
        => new(
            meal.Id,
            meal.Name,
            meal.MealType,
            meal.ConsumedAt,
            meal.Notes,
            meal.IsSaved,
            meal.MealItems.Select( item => MealItemResponse.From(item) ).ToList());
}
