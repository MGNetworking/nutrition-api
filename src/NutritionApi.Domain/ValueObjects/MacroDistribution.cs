namespace NutritionApi.Domain.ValueObjects;

/// <summary>
/// Value Object immuable — répartition cible des macronutriments en pourcentage de calories.
/// C'est un objectif de régime (ce que l'utilisateur devrait manger), pas une consommation réelle.
/// Invariant : la somme des trois pourcentages fait toujours 100.
/// </summary>
public sealed record MacroDistribution
{
    /// <summary>Pourcentage de calories venant des protéines.</summary>
    public int ProteinPercentage { get; }

    /// <summary>Pourcentage de calories venant des glucides.</summary>
    public int CarbPercentage { get; }

    /// <summary>Pourcentage de calories venant des lipides.</summary>
    public int FatPercentage { get; }

    /// <summary>Crée une répartition de macros — chaque pourcentage est strictement positif et la somme fait 100.</summary>
    /// <param name="proteinPercentage">% de calories venant des protéines.</param>
    /// <param name="carbPercentage">% de calories venant des glucides.</param>
    /// <param name="fatPercentage">% de calories venant des lipides.</param>
    /// <exception cref="ArgumentOutOfRangeException">Un pourcentage est négatif ou nul.</exception>
    /// <exception cref="ArgumentException">La somme des trois pourcentages n'est pas égale à 100.</exception>
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
