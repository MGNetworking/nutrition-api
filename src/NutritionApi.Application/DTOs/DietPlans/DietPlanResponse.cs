using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.DietPlans;

public record DietPlanResponse(
    Guid Id,
    string Name,
    DietType DietType,
    Goal Goal,
    float? TargetWeight,
    MacroDistributionDto MacroDistribution,
    bool IsTemplate
);
