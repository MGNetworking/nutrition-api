namespace NutritionApi.Application.DTOS.Meals;

using NutritionApi.Domain.Enums;

public record UpdateMealRequest(
    string? Name,
    MealType? MealType,
    string? Notes,
    DateTime? ConsumedAt,
    bool? IsSaved
);
