namespace NutritionApi.Domain.Enums;

/// <summary>Niveau d'activité physique de l'utilisateur — détermine le facteur d'activité appliqué au BMR pour calculer le TDEE.</summary>
public enum ActivityLevel
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Sédentaire — peu ou pas d'exercice.</summary>
    Sedentary = 1,

    /// <summary>Légèrement actif — exercice léger 1 à 3 jours par semaine.</summary>
    LightlyActive = 2,

    /// <summary>Modérément actif — exercice modéré 3 à 5 jours par semaine.</summary>
    ModeratelyActive = 3,

    /// <summary>Très actif — exercice intense 6 à 7 jours par semaine.</summary>
    VeryActive = 4,

    /// <summary>Extrêmement actif — travail physique ou entraînement biquotidien.</summary>
    ExtremelyActive = 5
}
