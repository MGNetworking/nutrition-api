namespace NutritionApi.Infrastructure.Jobs.OffImport;

using NutritionApi.Domain.Enums;

/// <summary>
/// Produit Open Food Facts normalisé — champs extraits et convertis, prêts à créer ou
/// rafraîchir un <see cref="Domain.Entity.FoodItem"/>.
/// </summary>
/// <param name="OffId">Identifiant Open Food Facts (code-barres).</param>
/// <param name="Name">Nom du produit.</param>
/// <param name="CaloriesPer100g">Calories pour 100g (kcal).</param>
/// <param name="ProteinsPer100g">Protéines pour 100g (g), arrondies.</param>
/// <param name="CarbsPer100g">Glucides pour 100g (g), arrondis.</param>
/// <param name="FatsPer100g">Lipides pour 100g (g), arrondis.</param>
/// <param name="AllergensTags">Allergènes reconnus parmi les 14 officiels UE.</param>
public sealed record OffProduct(
    string OffId,
    string Name,
    float CaloriesPer100g,
    int ProteinsPer100g,
    int CarbsPer100g,
    int FatsPer100g,
    List<Allergen> AllergensTags);
