using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.FoodItems;

/// <summary>Aliment retourné par la recherche par mot-clé, avec ses valeurs nutritionnelles pour 100 g.</summary>
/// <param name="Id">Identifiant de l'aliment dans le catalogue.</param>
/// <param name="Name">Nom de l'aliment.</param>
/// <param name="CaloriesPer100g">Calories pour 100 g, en kcal.</param>
/// <param name="ProteinsPer100g">Protéines pour 100 g, en grammes.</param>
/// <param name="CarbsPer100g">Glucides pour 100 g, en grammes.</param>
/// <param name="FatsPer100g">Lipides pour 100 g, en grammes.</param>
/// <param name="AllergensTags">Allergènes de l'aliment (tags normalisés OpenFoodFacts).</param>
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
    /// <summary>Construit la réponse à partir de l'entité <see cref="FoodItem"/>.</summary>
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
