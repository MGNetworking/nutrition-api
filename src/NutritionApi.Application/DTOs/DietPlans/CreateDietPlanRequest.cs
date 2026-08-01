using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.DietPlans;

/// <summary>Requête de création d'un plan diététique personnel.</summary>
/// <param name="Name">Nom du plan.</param>
/// <param name="DietType">Type de diète du plan.</param>
/// <param name="Goal">Objectif du plan (perte, maintien ou prise de poids).</param>
/// <param name="TargetWeight">Poids cible en kilogrammes, ou <c>null</c> si non défini.</param>
/// <param name="MacroDistribution">Répartition des macronutriments en pourcentages.</param>
public record CreateDietPlanRequest(
    string Name,
    DietType DietType,
    Goal Goal,
    float? TargetWeight,
    MacroDistributionDto MacroDistribution
);
