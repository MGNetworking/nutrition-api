
namespace NutritionApi.Application.DTOS.FoodItems;

/// <summary>Requête de mise en favori d'un aliment du catalogue.</summary>
/// <param name="FoodItemId">Identifiant de l'aliment à sauvegarder en favori.</param>
public record SaveFoodItemRequest(Guid FoodItemId);
