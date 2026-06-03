using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.DietPlans;

public record CreateDietPlanRequest(
    string Name,
    DietType DietType,
    Goal Goal,
    float? TargetWeight,
    MacroDistributionDto MacroDistribution
);
