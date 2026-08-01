
namespace NutritionApi.Application.DTOS.Users;

/// <summary>Requête de modification d'une pesée existante.</summary>
/// <param name="Weight">Poids mesuré, en kilogrammes.</param>
/// <param name="MeasuredAt">Date de la pesée.</param>
public record UpdateWeightEntryRequest(float Weight, DateOnly MeasuredAt);
