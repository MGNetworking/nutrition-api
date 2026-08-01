
namespace NutritionApi.Application.DTOS.Meals;

using NutritionApi.Domain.Entity;

/// <summary>Aliment d'un repas avec ses valeurs nutritionnelles calculées pour la quantité consommée.</summary>
/// <param name="Id">Identifiant de l'item de repas.</param>
/// <param name="FoodItemId">Identifiant de l'aliment référencé dans le catalogue.</param>
/// <param name="FoodName">Nom de l'aliment.</param>
/// <param name="Quantity">Quantité consommée, en grammes.</param>
/// <param name="Calories">Calories pour la quantité consommée, en kcal.</param>
/// <param name="Proteins">Protéines pour la quantité consommée, en grammes.</param>
/// <param name="Carbs">Glucides pour la quantité consommée, en grammes.</param>
/// <param name="Fats">Lipides pour la quantité consommée, en grammes.</param>
public record MealItemResponse(
    Guid Id,
    Guid FoodItemId,
    string FoodName,
    float Quantity,
    float Calories,
    float Proteins,
    float Carbs,
    float Fats
)
{
    /// <summary>Construit la réponse à partir de l'entité <see cref="MealItem"/>.</summary>
    public static MealItemResponse From(MealItem mealItem)
        => new(mealItem.Id,
            mealItem.FoodItemId,
            mealItem.FoodItem!.Name,
            mealItem.Quantity,
            mealItem.Nutrition.Calories,
            mealItem.Nutrition.Proteins,
            mealItem.Nutrition.Carbs,
            mealItem.Nutrition.Fats);
}
