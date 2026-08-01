using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Domain.ValueObjects;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Diets;

/// <summary>Régime en cours ou archivé, créé à partir d'un snapshot de plan diététique.</summary>
/// <param name="Id">Identifiant du régime.</param>
/// <param name="Name">Nom du régime, hérité du plan au lancement.</param>
/// <param name="DietType">Type de diète du régime.</param>
/// <param name="Goal">Objectif du régime (perte, maintien ou prise de poids).</param>
/// <param name="TargetWeight">Poids cible en kilogrammes, ou <c>null</c> si non défini.</param>
/// <param name="CalorieTarget">Objectif calorique quotidien calculé au lancement, en kcal.</param>
/// <param name="MacroDistribution">Répartition des macronutriments en pourcentages.</param>
/// <param name="Status">Statut du régime (Active ou Archived).</param>
/// <param name="StartDate">Date de lancement du régime.</param>
/// <param name="EndDate">Date de fin du régime, ou <c>null</c> s'il est encore actif.</param>
public record DietResponse(
    Guid Id,
    string Name,
    DietType DietType,
    Goal Goal,
    float? TargetWeight,
    float CalorieTarget,
    MacroDistributionDto MacroDistribution,
    DietStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate
)
{
    /// <summary>Construit la réponse à partir de l'entité <see cref="Diet"/>.</summary>
    public static DietResponse From(Diet diet)
        => new(
            diet.Id,
            diet.Name,
            diet.DietType,
            diet.Goal,
            diet.TargetWeight,
            diet.CalorieTarget,
            MacroDistributionDto.From(diet.MacroDistribution),
            diet.StatusDiet,
            diet.StartDate,
            diet.EndDate);
}
