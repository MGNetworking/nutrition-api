namespace NutritionApi.Infrastructure.Jobs.OffImport;

using System.Text.Json;
using System.Text.Json.Serialization;
using NutritionApi.Domain.Enums;

/// <summary>
/// Traduit une ligne JSON du dump Open Food Facts en <see cref="OffProduct"/> normalisé :
/// extraction des champs, arrondi des valeurs nutritionnelles, correspondance des allergènes.
/// </summary>
public static class OffProductMapper
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // OFF sérialise parfois les nombres sous forme de chaînes ("12.4").
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    // Tags Open Food Facts → allergènes officiels UE. Tout tag absent de cette table est ignoré.
    private static readonly Dictionary<string, Allergen> AllergenByTag = new()
    {
        ["en:gluten"] = Allergen.Gluten,
        ["en:crustaceans"] = Allergen.Crustaceans,
        ["en:eggs"] = Allergen.Eggs,
        ["en:fish"] = Allergen.Fish,
        ["en:peanuts"] = Allergen.Peanuts,
        ["en:soybeans"] = Allergen.Soybeans,
        ["en:milk"] = Allergen.Milk,
        ["en:nuts"] = Allergen.Nuts,
        ["en:celery"] = Allergen.Celery,
        ["en:mustard"] = Allergen.Mustard,
        ["en:sesame-seeds"] = Allergen.SesameSeeds,
        ["en:sulphur-dioxide-and-sulphites"] = Allergen.SulphurDioxide,
        ["en:lupin"] = Allergen.Lupin,
        ["en:molluscs"] = Allergen.Molluscs
    };

    /// <summary>
    /// Convertit une ligne JSON en <see cref="OffProduct"/>, ou retourne <c>null</c> si le produit
    /// est inexploitable (JSON invalide, code ou nom absent, valeur nutritionnelle négative).
    /// </summary>
    /// <param name="jsonLine">Une ligne du dump JSONL Open Food Facts.</param>
    /// <returns>Le produit normalisé, ou <c>null</c> s'il doit être ignoré.</returns>
    public static OffProduct? TryMap(string jsonLine)
    {
        RawProduct? raw;
        try
        {
            raw = JsonSerializer.Deserialize<RawProduct>(jsonLine, Options);
        }
        catch (JsonException)
        {
            return null;
        }

        if (raw is null)
            return null;

        var offId = raw.Code?.Trim();
        var name = raw.ProductName?.Trim();

        // Un produit sans code-barres ou sans nom n'est pas exploitable par le catalogue.
        if (string.IsNullOrWhiteSpace(offId) || string.IsNullOrWhiteSpace(name))
            return null;

        var calories = raw.Nutriments?.EnergyKcal ?? 0f;
        var proteins = ToInt(raw.Nutriments?.Proteins);
        var carbs = ToInt(raw.Nutriments?.Carbs);
        var fats = ToInt(raw.Nutriments?.Fat);

        // Valeurs aberrantes (négatives) → produit ignoré plutôt que faussé.
        if (calories < 0 || proteins < 0 || carbs < 0 || fats < 0)
            return null;

        return new OffProduct(offId, name, calories, proteins, carbs, fats, MapAllergens(raw.AllergensTags));
    }

    private static int ToInt(float? value)
        => value is null ? 0 : (int)Math.Round(value.Value, MidpointRounding.AwayFromZero);

    private static List<Allergen> MapAllergens(List<string>? tags)
    {
        if (tags is null)
            return [];

        var allergens = new List<Allergen>();
        foreach (var tag in tags)
        {
            if (tag is null)
                continue;

            if (AllergenByTag.TryGetValue(tag.Trim().ToLowerInvariant(), out var allergen)
                && !allergens.Contains(allergen))
            {
                allergens.Add(allergen);
            }
        }

        return allergens;
    }

    private sealed class RawProduct
    {
        [JsonPropertyName("code")] public string? Code { get; set; }
        [JsonPropertyName("product_name")] public string? ProductName { get; set; }
        [JsonPropertyName("allergens_tags")] public List<string>? AllergensTags { get; set; }
        [JsonPropertyName("nutriments")] public RawNutriments? Nutriments { get; set; }
    }

    private sealed class RawNutriments
    {
        [JsonPropertyName("energy-kcal_100g")] public float? EnergyKcal { get; set; }
        [JsonPropertyName("proteins_100g")] public float? Proteins { get; set; }
        [JsonPropertyName("carbohydrates_100g")] public float? Carbs { get; set; }
        [JsonPropertyName("fat_100g")] public float? Fat { get; set; }
    }
}
