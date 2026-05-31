using NutritionApi.Domain.Enums;

namespace NutritionApi.Domain.Entity;

public class FoodItem
{
    public Guid Id { get; init; }
    public string OffId { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public float CaloriesPer100g { get; private set; }
    public int ProteinsPer100g { get; private set; }
    public int CarbsPer100g { get; private set; }
    public int FatsPer100g { get; private set; }
    public List<Allergen> AllergensTags { get; private set; } = new List<Allergen>();
    public DateTime CachedAt { get; private set; }

    // Pour EF Core uniquement
    private FoodItem() { }

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
