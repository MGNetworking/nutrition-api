namespace NutritionApi.Application.DTOS.Meals;

using NutritionApi.Domain.Enums;

public record MealResponse(
    Guid Id,
    string Name,
    MealType MealType,
    DateTime ConsumedAt,
    string? Notes,
    bool IsSaved,
    List<MealItemResponse> Items
);
