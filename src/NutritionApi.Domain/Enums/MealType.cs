namespace NutritionApi.Domain.Enums;

/// <summary>Type de repas enregistré par l'utilisateur.</summary>
public enum MealType
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Petit-déjeuner.</summary>
    Breakfast = 1,

    /// <summary>Déjeuner.</summary>
    Lunch = 2,

    /// <summary>Dîner.</summary>
    Dinner = 3,

    /// <summary>Collation.</summary>
    Snack = 4
}
