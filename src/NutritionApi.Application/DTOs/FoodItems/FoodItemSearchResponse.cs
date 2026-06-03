using NutritionApi.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NutritionApi.Application.DTOS.FoodItems;

public record FoodItemSearchResponse(
    Guid Id,
    string Name,
    float CaloriesPer100g,
    float ProteinsPer100g,
    float CarbsPer100g,
    float FatsPer100g,
    List<Allergen> AllergensTags
);
