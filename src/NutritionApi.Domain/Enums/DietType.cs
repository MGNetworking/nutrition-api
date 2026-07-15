namespace NutritionApi.Domain.Enums;

/// <summary>Type de régime alimentaire porté par un DietPlan ou une Diet.</summary>
public enum DietType
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Régime équilibré.</summary>
    Balanced = 1,

    /// <summary>Régime hyperprotéiné.</summary>
    HighProtein = 2,

    /// <summary>Régime cétogène — très pauvre en glucides, riche en lipides.</summary>
    Keto = 3,

    /// <summary>Régime méditerranéen.</summary>
    Mediterranean = 4,

    /// <summary>Régime pauvre en glucides.</summary>
    LowCarb = 5,

    /// <summary>Régime végétarien.</summary>
    Vegetarian = 6,

    /// <summary>Régime végan.</summary>
    Vegan = 7,

    /// <summary>Régime personnalisé par l'utilisateur.</summary>
    Custom = 8
}
