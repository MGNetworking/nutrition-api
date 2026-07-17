using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.DietPlans;

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
    public static DietPlanResponse From(DietPlan diet) 
        => new(diet.Id, 
            diet.Name, 
            diet.DietType, 
            diet.Goal, 
            diet.TargetWeight, 
            MacroDistributionDto.From(diet.MacroDistribution), 
            diet.IsTemplate);
}
