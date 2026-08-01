namespace NutritionApi.Domain.ValueObjects;

/// <summary>
/// Value Object immuable — résultat du calcul nutritionnel personnalisé (moteur BMR/TDEE).
/// N'est jamais persisté — calculé à la demande dans la couche Application.
/// </summary>
public sealed record NutritionNeeds
{
    /// <summary>Métabolisme de base (kcal).</summary>
    public float Bmr { get; }

    /// <summary>Dépense énergétique totale journalière (kcal) — BMR × facteur d'activité.</summary>
    public float Tdee { get; }

    /// <summary>Objectif calorique ajusté selon le Goal (kcal).</summary>
    public float TargetCalories { get; }

    /// <summary>Répartition cible des macros en %.</summary>
    public MacroDistribution MacroDistribution { get; }

    /// <summary>Crée un résultat de calcul nutritionnel — toutes les valeurs caloriques sont strictement positives.</summary>
    /// <param name="bmr">Métabolisme de base (kcal).</param>
    /// <param name="tdee">Dépense énergétique totale (kcal).</param>
    /// <param name="targetCalories">Objectif calorique selon le Goal (kcal).</param>
    /// <param name="macroDistribution">Répartition cible des macros.</param>
    /// <exception cref="ArgumentOutOfRangeException">bmr, tdee ou targetCalories est négatif ou nul.</exception>
    /// <exception cref="ArgumentNullException">macroDistribution est null.</exception>
    public NutritionNeeds(float bmr, float tdee, float targetCalories, MacroDistribution macroDistribution)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bmr);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tdee);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetCalories);
        ArgumentNullException.ThrowIfNull(macroDistribution);

        Bmr = bmr;
        Tdee = tdee;
        TargetCalories = targetCalories;
        MacroDistribution = macroDistribution;
    }
}
