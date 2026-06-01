namespace NutritionApi.Application.DTOS.DietPlans;

using NutritionApi.Domain.Enums;

public record CreateDietPlanRequest(
    string Name,
    DietType DietType,
    Goal Goal,
    float? TargetWeight,
    MacroDistributionDto MacroDistribution
);
