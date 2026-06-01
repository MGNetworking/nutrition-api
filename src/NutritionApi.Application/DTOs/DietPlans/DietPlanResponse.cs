namespace NutritionApi.Application.DTOS.DietPlans;

using NutritionApi.Domain.Enums;

public record DietPlanResponse(
    Guid Id,
    string Name,
    DietType DietType,
    Goal Goal,
    float? TargetWeight,
    MacroDistributionDto MacroDistribution,
    bool IsTemplate
);
