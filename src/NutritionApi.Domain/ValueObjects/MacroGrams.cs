namespace NutritionApi.Domain.ValueObjects;

public sealed record MacroGrams
{
    public float ProteinG { get; }
    public float CarbG { get; }
    public float FatG { get; }

    public MacroGrams(float proteinG, float carbG, float fatG)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(proteinG);
        ArgumentOutOfRangeException.ThrowIfNegative(carbG);
        ArgumentOutOfRangeException.ThrowIfNegative(fatG);

        ProteinG = proteinG;
        CarbG = carbG;
        FatG = fatG;
    }
}
