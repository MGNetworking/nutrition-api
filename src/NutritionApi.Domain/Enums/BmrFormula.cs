namespace NutritionApi.Domain.Enums;

/// <summary>Formule de calcul du métabolisme de base (BMR) utilisée par le moteur de calcul nutritionnel.</summary>
public enum BmrFormula
{
    /// <summary>Formule Mifflin-St Jeor (1990) — formule de référence par défaut.</summary>
    MifflinStJeor,

    /// <summary>Formule Harris-Benedict révisée (1984).</summary>
    HarrisBenedict
}
