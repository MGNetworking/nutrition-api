
using NutritionApi.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NutritionApi.Application.DTOS.Meals;

public record CreateMealRequest(
    string Name,
    MealType MealType,
    DateTime ConsumedAt,
    string? Notes,
    bool IsSaved,
    List<MealItemRequest> Items
);
