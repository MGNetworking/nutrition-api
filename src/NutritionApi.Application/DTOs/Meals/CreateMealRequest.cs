namespace NutritionApi.Application.DTOS.Meals;

using NutritionApi.Domain.Enums;

public record CreateMealRequest(
    string Name,
    MealType MealType,
    DateTime ConsumedAt,
    string? Notes,
    bool IsSaved,
    List<MealItemRequest> Items
);
