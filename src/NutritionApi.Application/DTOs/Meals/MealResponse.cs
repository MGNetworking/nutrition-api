using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Meals;

public record MealResponse(
    Guid Id,
    string Name,
    MealType MealType,
    DateTime ConsumedAt,
    string? Notes,
    bool IsSaved,
    List<MealItemResponse> Items
);
