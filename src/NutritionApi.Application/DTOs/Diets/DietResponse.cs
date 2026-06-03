using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Diets;

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
);
