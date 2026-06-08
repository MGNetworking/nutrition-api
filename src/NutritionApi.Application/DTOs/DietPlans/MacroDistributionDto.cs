
using NutritionApi.Domain.ValueObjects;
namespace NutritionApi.Application.DTOS.DietPlans;

public record MacroDistributionDto(float ProteinPct, float CarbPct, float FatPct)
{
    public static MacroDistributionDto From(MacroDistribution macro)
            => new(macro.ProteinPercentage, macro.CarbPercentage, macro.FatPercentage);
}
