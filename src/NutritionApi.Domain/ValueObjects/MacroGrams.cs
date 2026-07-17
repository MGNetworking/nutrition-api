namespace NutritionApi.Domain.ValueObjects;

/// <summary>
/// Value Object immuable — quantités de macronutriments exprimées en grammes,
/// converties depuis un objectif calorique et une répartition en pourcentages.
/// </summary>
public sealed record MacroGrams
{
    /// <summary>Protéines en grammes.</summary>
    public float ProteinG { get; }

    /// <summary>Glucides en grammes.</summary>
    public float CarbG { get; }

    /// <summary>Lipides en grammes.</summary>
    public float FatG { get; }

    /// <summary>Crée une répartition de macros en grammes — toutes les valeurs sont positives ou nulles.</summary>
    /// <param name="proteinG">Protéines en grammes.</param>
    /// <param name="carbG">Glucides en grammes.</param>
    /// <param name="fatG">Lipides en grammes.</param>
    /// <exception cref="ArgumentOutOfRangeException">Une valeur est négative.</exception>
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
