
namespace NutritionApi.Application.DTOS.Users;

/// <summary>Requête d'ajout d'une pesée.</summary>
/// <param name="Weight">Poids mesuré, en kilogrammes.</param>
/// <param name="MeasuredAt">Date de la pesée, ou <c>null</c> pour la date du jour.</param>
public record AddWeightEntryRequest(float Weight, DateOnly? MeasuredAt = null);
