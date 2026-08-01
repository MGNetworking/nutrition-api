
using NutritionApi.Domain.Enums;
using System.Diagnostics.CodeAnalysis;

namespace NutritionApi.Application.DTOS.Meals;

/// <summary>Requête de création d'un repas, ponctuel ou sauvegardé.</summary>
/// <param name="Name">Nom du repas.</param>
/// <param name="MealType">Type de repas (petit-déjeuner, déjeuner, dîner, collation).</param>
/// <param name="ConsumedAt">Date et heure de consommation.</param>
/// <param name="Notes">Notes libres, ou <c>null</c>.</param>
/// <param name="IsSaved"><c>true</c> pour sauvegarder le repas et le réutiliser plus tard (quota selon le tier).</param>
/// <param name="Items">Aliments composant le repas.</param>
public record CreateMealRequest(
    string Name,
    MealType MealType,
    DateTime ConsumedAt,
    string? Notes,
    bool IsSaved,
    List<MealItemRequest> Items
);
