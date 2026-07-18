using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.Meals;

/// <summary>Repas de l'utilisateur avec ses aliments et leurs valeurs nutritionnelles.</summary>
/// <param name="Id">Identifiant du repas.</param>
/// <param name="Name">Nom du repas.</param>
/// <param name="MealType">Type de repas (petit-déjeuner, déjeuner, dîner, collation).</param>
/// <param name="ConsumedAt">Date et heure de consommation.</param>
/// <param name="Notes">Notes libres, ou <c>null</c>.</param>
/// <param name="IsSaved"><c>true</c> si le repas est sauvegardé pour réutilisation.</param>
/// <param name="Items">Aliments composant le repas.</param>
public record MealResponse(
    Guid Id,
    string Name,
    MealType MealType,
    DateTime ConsumedAt,
    string? Notes,
    bool IsSaved,
    List<MealItemResponse> Items
)
{
    /// <summary>Construit la réponse à partir de l'entité <see cref="Meal"/> et de ses items.</summary>
    public static MealResponse From(Meal meal)
        => new(
            meal.Id,
            meal.Name,
            meal.MealType,
            meal.ConsumedAt,
            meal.Notes,
            meal.IsSaved,
            meal.MealItems.Select( item => MealItemResponse.From(item) ).ToList());
}
