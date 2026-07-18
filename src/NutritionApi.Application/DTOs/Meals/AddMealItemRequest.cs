
namespace NutritionApi.Application.DTOS.Meals;

/// <summary>Requête d'ajout d'un aliment à un repas existant.</summary>
/// <param name="FoodItemId">Identifiant de l'aliment du catalogue.</param>
/// <param name="Quantity">Quantité consommée, en grammes.</param>
public record AddMealItemRequest(Guid FoodItemId, float Quantity);
