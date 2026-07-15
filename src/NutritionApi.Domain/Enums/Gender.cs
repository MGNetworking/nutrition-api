namespace NutritionApi.Domain.Enums;

/// <summary>Genre de l'utilisateur — utilisé pour le calcul du métabolisme de base (BMR).</summary>
public enum Gender
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Homme.</summary>
    Male = 1,

    /// <summary>Femme.</summary>
    Female = 2,

    /// <summary>Autre.</summary>
    Other = 3
}
