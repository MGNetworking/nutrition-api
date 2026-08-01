using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.FoodItems;

/// <summary>Aliment favori d'un utilisateur, enrichi des informations de l'aliment du catalogue.</summary>
/// <param name="Id">Identifiant de l'entrée favori.</param>
/// <param name="FoodItemId">Identifiant de l'aliment référencé dans le catalogue.</param>
/// <param name="Name">Nom de l'aliment.</param>
/// <param name="CaloriesPer100g">Calories pour 100 g, en kcal.</param>
/// <param name="SavedAt">Date de mise en favori.</param>
public record SavedFoodItemResponse(
    Guid Id,
    Guid FoodItemId,
    string Name,
    float CaloriesPer100g,
    DateTime SavedAt
)
{
    /// <summary>Construit la réponse à partir du favori et de l'aliment du catalogue associé.</summary>
    public static SavedFoodItemResponse From(SavedFoodItem saved, FoodItem foodItem)
    => new(saved.Id, saved.FoodItemId, foodItem.Name, foodItem.CaloriesPer100g, saved.SavedAt);
}

