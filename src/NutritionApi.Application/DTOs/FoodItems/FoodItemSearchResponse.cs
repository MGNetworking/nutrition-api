using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.FoodItems;

public record FoodItemSearchResponse(
    Guid Id,
    string Name,
    float CaloriesPer100g,
    float ProteinsPer100g,
    float CarbsPer100g,
    float FatsPer100g,
    List<Allergen> AllergensTags
)
{
    public static FoodItemSearchResponse From(FoodItem foodItem)
    => new(
        Id: foodItem.Id,
        Name: foodItem.Name,
        CaloriesPer100g: foodItem.CaloriesPer100g,
        ProteinsPer100g: foodItem.ProteinsPer100g,
        CarbsPer100g: foodItem.CarbsPer100g,
        FatsPer100g: foodItem.FatsPer100g,
        AllergensTags: foodItem.AllergensTags
    );
}
