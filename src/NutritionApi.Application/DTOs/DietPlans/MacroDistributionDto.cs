namespace NutritionApi.Application.DTOS.DietPlans;

using NutritionApi.Domain.ValueObjects;

public record MacroDistributionDto(int ProteinPct, int CarbPct, int FatPct)
{
    public static MacroDistributionDto From(MacroDistribution macro)
            => new(macro.ProteinPercentage, macro.CarbPercentage, macro.FatPercentage);
}
