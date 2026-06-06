using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Meals;

public record UpdateMealRequest(
    string? Name,
    MealType? MealType,
    string? Notes,
    DateTime? ConsumedAt,
    bool? IsSaved
);
