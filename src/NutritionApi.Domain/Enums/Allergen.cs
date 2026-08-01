namespace NutritionApi.Domain.Enums;

/// <summary>
/// Les 14 allergènes officiels de l'Union Européenne, calqués sur le référentiel Open Food Facts
/// (ex: <c>en:gluten</c>, <c>en:milk</c>) — permet une comparaison fiable entre les allergies
/// de l'utilisateur et les allergènes d'un aliment.
/// </summary>
public enum Allergen
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Gluten (blé, seigle, orge, avoine).</summary>
    Gluten = 1,

    /// <summary>Crustacés.</summary>
    Crustaceans = 2,

    /// <summary>Œufs.</summary>
    Eggs = 3,

    /// <summary>Poisson.</summary>
    Fish = 4,

    /// <summary>Arachides.</summary>
    Peanuts = 5,

    /// <summary>Soja.</summary>
    Soybeans = 6,

    /// <summary>Lait.</summary>
    Milk = 7,

    /// <summary>Fruits à coque (amandes, noisettes, noix…).</summary>
    Nuts = 8,

    /// <summary>Céleri.</summary>
    Celery = 9,

    /// <summary>Moutarde.</summary>
    Mustard = 10,

    /// <summary>Graines de sésame.</summary>
    SesameSeeds = 11,

    /// <summary>Anhydride sulfureux et sulfites.</summary>
    SulphurDioxide = 12,

    /// <summary>Lupin.</summary>
    Lupin = 13,

    /// <summary>Mollusques.</summary>
    Molluscs = 14
}
