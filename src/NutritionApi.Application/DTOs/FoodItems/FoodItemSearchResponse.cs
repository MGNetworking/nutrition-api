namespace NutritionApi.Application.DTOS.FoodItems;

using NutritionApi.Domain.Enums;

public record FoodItemSearchResponse(
    Guid Id,
    string Name,
    float CaloriesPer100g,
    float ProteinsPer100g,
    float CarbsPer100g,
    float FatsPer100g,
    List<Allergen> AllergensTags
);
