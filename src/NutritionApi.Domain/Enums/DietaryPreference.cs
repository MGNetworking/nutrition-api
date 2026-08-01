namespace NutritionApi.Domain.Enums;

/// <summary>
/// Régimes alimentaires déclarés par l'utilisateur, en liste fermée — une valeur libre rendrait
/// « vegan », « Vegan » et « végétalien » incomparables entre deux profils.
/// </summary>
/// <remarks>
/// À ne pas confondre avec <see cref="Allergen"/> : une préférence est un choix, une allergie est
/// une contrainte médicale. <c>GlutenFree</c> et <c>LactoseFree</c> recoupent des allergènes sans
/// s'y substituer — un utilisateur allergique au lait déclare <see cref="Allergen.Milk"/>, pas une
/// préférence.
/// </remarks>
public enum DietaryPreference
{
    /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
    Unknown = 0,

    /// <summary>Sans viande ni poisson.</summary>
    Vegetarian = 1,

    /// <summary>Sans aucun produit d'origine animale.</summary>
    Vegan = 2,

    /// <summary>Sans viande, poisson et fruits de mer autorisés.</summary>
    Pescetarian = 3,

    /// <summary>Conforme aux prescriptions alimentaires musulmanes.</summary>
    Halal = 4,

    /// <summary>Conforme aux prescriptions alimentaires juives.</summary>
    Kosher = 5,

    /// <summary>Sans gluten, par choix — l'intolérance relève de <see cref="Allergen.Gluten"/>.</summary>
    GlutenFree = 6,

    /// <summary>Sans lactose, par choix — l'allergie relève de <see cref="Allergen.Milk"/>.</summary>
    LactoseFree = 7
}
