namespace NutritionApi.Domain.ValueObjects;

public sealed record MacroDistribution
{
    public int ProteinPercentage { get; }
    public int CarbPercentage { get; }
    public int FatPercentage { get; }
    public MacroDistribution(int proteinPercentage, int carbPercentage, int fatPercentage)
    {

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(proteinPercentage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(carbPercentage);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(fatPercentage);

        if (proteinPercentage + carbPercentage + fatPercentage != 100)
            throw new ArgumentException($"Macro percentages must sum to 100. Received: {proteinPercentage + carbPercentage + fatPercentage}.");

        this.ProteinPercentage = proteinPercentage;
        this.CarbPercentage = carbPercentage;
        this.FatPercentage = fatPercentage;
    }

}
