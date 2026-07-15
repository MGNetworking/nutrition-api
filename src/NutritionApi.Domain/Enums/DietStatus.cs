namespace NutritionApi.Domain.Enums;

/// <summary>Statut du cycle de vie d'une Diet — une seule Diet Active par utilisateur.</summary>
public enum DietStatus
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Régime en cours.</summary>
    Active = 1,

    /// <summary>Régime terminé normalement par l'utilisateur.</summary>
    Archived = 2,

    /// <summary>Régime abandonné en cours de route.</summary>
    Cancelled = 3
}
