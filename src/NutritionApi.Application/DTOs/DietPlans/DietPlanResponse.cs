using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.DietPlans;

/// <summary>Plan diététique, personnel ou template partagé.</summary>
/// <param name="Id">Identifiant du plan.</param>
/// <param name="Name">Nom du plan.</param>
/// <param name="DietType">Type de diète du plan.</param>
/// <param name="Goal">Objectif du plan (perte, maintien ou prise de poids).</param>
/// <param name="TargetWeight">Poids cible en kilogrammes, ou <c>null</c> si non défini.</param>
/// <param name="MacroDistribution">Répartition des macronutriments en pourcentages.</param>
/// <param name="IsTemplate"><c>true</c> si le plan est un template partagé, <c>false</c> pour un plan personnel.</param>
public record DietPlanResponse(
    Guid Id,
    string Name,
    DietType DietType,
    Goal Goal,
    float? TargetWeight,
    MacroDistributionDto MacroDistribution,
    bool IsTemplate
)
{
    /// <summary>Construit la réponse à partir de l'entité <see cref="DietPlan"/>.</summary>
    public static DietPlanResponse From(DietPlan diet)
        => new(diet.Id, 
            diet.Name, 
            diet.DietType, 
            diet.Goal, 
            diet.TargetWeight, 
            MacroDistributionDto.From(diet.MacroDistribution), 
            diet.IsTemplate);
}
