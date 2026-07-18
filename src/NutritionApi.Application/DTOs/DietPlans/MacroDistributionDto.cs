namespace NutritionApi.Application.DTOS.DietPlans;

using NutritionApi.Domain.ValueObjects;

/// <summary>Répartition des macronutriments en pourcentages (la somme doit faire 100).</summary>
/// <param name="ProteinPct">Part des protéines, en pourcentage.</param>
/// <param name="CarbPct">Part des glucides, en pourcentage.</param>
/// <param name="FatPct">Part des lipides, en pourcentage.</param>
public record MacroDistributionDto(int ProteinPct, int CarbPct, int FatPct)
{
    /// <summary>Construit le DTO à partir du value object <see cref="MacroDistribution"/>.</summary>
    public static MacroDistributionDto From(MacroDistribution macro)
            => new(macro.ProteinPercentage, macro.CarbPercentage, macro.FatPercentage);
}
