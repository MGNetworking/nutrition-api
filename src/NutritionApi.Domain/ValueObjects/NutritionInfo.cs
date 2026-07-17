namespace NutritionApi.Domain.ValueObjects;

/// <summary>
/// Value Object immuable — valeurs nutritionnelles réelles d'un aliment pour une quantité donnée.
/// Snapshot calculé une fois à la création du MealItem (<c>Per100g × Quantity / 100</c>)
/// puis stocké définitivement — l'historique ne change jamais rétroactivement.
/// </summary>
public sealed record NutritionInfo
{
    /// <summary>Calories consommées (kcal).</summary>
    public float Calories { get; }

    /// <summary>Protéines consommées (grammes).</summary>
    public int Proteins { get; }

    /// <summary>Glucides consommés (grammes).</summary>
    public int Carbs { get; }

    /// <summary>Lipides consommés (grammes).</summary>
    public int Fats { get; }

    /// <summary>Crée un snapshot nutritionnel — toutes les valeurs sont positives ou nulles.</summary>
    /// <param name="calories">Calories (kcal).</param>
    /// <param name="proteins">Protéines (g).</param>
    /// <param name="carbs">Glucides (g).</param>
    /// <param name="fats">Lipides (g).</param>
    /// <exception cref="ArgumentOutOfRangeException">Une valeur est négative.</exception>
    public NutritionInfo(float calories, int proteins, int carbs, int fats)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(calories);
        ArgumentOutOfRangeException.ThrowIfNegative(proteins);
        ArgumentOutOfRangeException.ThrowIfNegative(carbs);
        ArgumentOutOfRangeException.ThrowIfNegative(fats);

        Calories = calories;
        Proteins = proteins;
        Carbs = carbs;
        Fats = fats;
    }
}
