
namespace NutritionApi.Application.DTOS.Meals;

/// <summary>Aliment et quantité composant un repas à la création.</summary>
/// <param name="FoodItemId">Identifiant de l'aliment du catalogue.</param>
/// <param name="Quantity">Quantité consommée, en grammes.</param>
public record MealItemRequest(Guid FoodItemId, float Quantity);
