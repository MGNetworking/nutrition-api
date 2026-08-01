using NutritionApi.Domain.Enums;

namespace NutritionApi.Domain.Entity;

/// <summary>
/// Agrégat racine — table de référence locale des données alimentaires issues d'Open Food Facts,
/// partagée entre tous les utilisateurs. Couvre les ingrédients bruts comme les produits finis ;
/// les valeurs nutritionnelles sont exprimées pour 100g.
/// </summary>
public class FoodItem
{
    /// <summary>Identifiant interne de l'aliment.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant Open Food Facts (code-barres).</summary>
    public string OffId { get; private set; } = null!;

    /// <summary>Nom de l'aliment.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Calories pour 100g (kcal).</summary>
    public float CaloriesPer100g { get; private set; }

    /// <summary>Protéines pour 100g (grammes).</summary>
    public int ProteinsPer100g { get; private set; }

    /// <summary>Glucides pour 100g (grammes).</summary>
    public int CarbsPer100g { get; private set; }

    /// <summary>Lipides pour 100g (grammes).</summary>
    public int FatsPer100g { get; private set; }

    /// <summary>Allergènes normalisés Open Food Facts (14 allergènes officiels UE).</summary>
    public List<Allergen> AllergensTags { get; private set; } = new List<Allergen>();

    /// <summary>Date de récupération des données depuis Open Food Facts (UTC).</summary>
    public DateTime CachedAt { get; private set; }

    // Pour EF Core uniquement
    private FoodItem() { }

    /// <summary>Crée un aliment de référence à partir des données Open Food Facts — <see cref="CachedAt"/> est fixé à maintenant (UTC).</summary>
    /// <param name="offId">Identifiant Open Food Facts (code-barres).</param>
    /// <param name="name">Nom de l'aliment.</param>
    /// <param name="caloriesPer100g">Calories pour 100g (kcal).</param>
    /// <param name="proteinsPer100g">Protéines pour 100g (g).</param>
    /// <param name="carbsPer100g">Glucides pour 100g (g).</param>
    /// <param name="fatsPer100g">Lipides pour 100g (g).</param>
    /// <param name="allergensTags">Allergènes normalisés OFF.</param>
    /// <exception cref="ArgumentException">offId ou name est null ou vide.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Une valeur nutritionnelle est négative.</exception>
    /// <exception cref="ArgumentNullException">allergensTags est null.</exception>
    public FoodItem(
        string offId,
        string name,
        float caloriesPer100g,
        int proteinsPer100g,
        int carbsPer100g,
        int fatsPer100g,
        List<Allergen> allergensTags)
    {
        Id = Guid.NewGuid();
        ArgumentException.ThrowIfNullOrWhiteSpace(offId);
        OffId = offId;

        SetName(name);
        SetCaloriesPer100g(caloriesPer100g);
        SetProteinsPer100g(proteinsPer100g);
        SetCarbsPer100g(carbsPer100g);
        SetFatsPer100g(fatsPer100g);
        SetAllergensTags(allergensTags);

        CachedAt = DateTime.UtcNow;
    }

    /// <summary>Rafraîchit les données depuis le job d'import quotidien Open Food Facts — remplace toutes les valeurs nutritionnelles et met à jour <see cref="CachedAt"/>.</summary>
    /// <param name="name">Nom de l'aliment.</param>
    /// <param name="caloriesPer100g">Calories pour 100g (kcal).</param>
    /// <param name="proteinsPer100g">Protéines pour 100g (g).</param>
    /// <param name="carbsPer100g">Glucides pour 100g (g).</param>
    /// <param name="fatsPer100g">Lipides pour 100g (g).</param>
    /// <param name="allergensTags">Allergènes normalisés OFF.</param>
    /// <exception cref="ArgumentException">name est null ou vide.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Une valeur nutritionnelle est négative.</exception>
    /// <exception cref="ArgumentNullException">allergensTags est null.</exception>
    public void UpdateFromImport(
        string name,
        float caloriesPer100g,
        int proteinsPer100g,
        int carbsPer100g,
        int fatsPer100g,
        List<Allergen> allergensTags)
    {
        SetName(name);
        SetCaloriesPer100g(caloriesPer100g);
        SetProteinsPer100g(proteinsPer100g);
        SetCarbsPer100g(carbsPer100g);
        SetFatsPer100g(fatsPer100g);
        SetAllergensTags(allergensTags);
        CachedAt = DateTime.UtcNow;
    }

    private void SetName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    private void SetCaloriesPer100g(float caloriesPer100g)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(caloriesPer100g);
        CaloriesPer100g = caloriesPer100g;
    }

    private void SetProteinsPer100g(int proteinsPer100g)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(proteinsPer100g);
        ProteinsPer100g = proteinsPer100g;
    }

    private void SetCarbsPer100g(int carbsPer100g)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(carbsPer100g);
        CarbsPer100g = carbsPer100g;
    }

    private void SetFatsPer100g(int fatsPer100g)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fatsPer100g);
        FatsPer100g = fatsPer100g;
    }

    private void SetAllergensTags(List<Allergen> allergensTags)
    {
        ArgumentNullException.ThrowIfNull(allergensTags);
        AllergensTags = allergensTags;
    }
}
