namespace NutritionApi.Domain.Enums;

/// <summary>Objectif de l'utilisateur pour un régime — ajuste l'objectif calorique calculé depuis le TDEE.</summary>
public enum Goal
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Perte de poids — déficit calorique.</summary>
    WeightLoss = 1,

    /// <summary>Maintien du poids — objectif calorique égal au TDEE.</summary>
    Maintenance = 2,

    /// <summary>Prise de poids — surplus calorique.</summary>
    WeightGain = 3
}
