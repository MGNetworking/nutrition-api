using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Domain.ValueObjects;
using NutritionApi.Domain.Entity;
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
)
{
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
