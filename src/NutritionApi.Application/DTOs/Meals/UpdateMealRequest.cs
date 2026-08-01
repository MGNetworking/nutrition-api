using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Meals;

/// <summary>Requête de modification partielle d'un repas — chaque propriété à <c>null</c> reste inchangée.</summary>
/// <param name="Name">Nouveau nom du repas, ou <c>null</c>.</param>
/// <param name="MealType">Nouveau type de repas, ou <c>null</c>.</param>
/// <param name="Notes">Nouvelles notes, ou <c>null</c>.</param>
/// <param name="ConsumedAt">Nouvelle date de consommation, ou <c>null</c>.</param>
/// <param name="IsSaved">Nouveau statut de sauvegarde, ou <c>null</c>.</param>
public record UpdateMealRequest(
    string? Name,
    MealType? MealType,
    string? Notes,
    DateTime? ConsumedAt,
    bool? IsSaved
);
