namespace NutritionApi.Infrastructure.Jobs.OffImport;

using System.Text.Json;
using System.Text.Json.Serialization;
using NutritionApi.Domain.Enums;

/// <summary>
/// Motif pour lequel une ligne du dump a été écartée (NTR-137).
/// </summary>
/// <remarks>
/// Le compte des lignes ignorées ne suffit pas à diagnostiquer un import : sur plusieurs millions
/// de lignes, deux millions de rejets peuvent venir d'un dump corrompu comme d'un mapping devenu
/// trop strict. Ces deux causes n'appellent pas la même réaction, et rien ne les distinguait.
/// </remarks>
public enum OffRejection
{
    /// <summary>La ligne a été convertie — aucun rejet.</summary>
    Aucun = 0,

    /// <summary>La ligne n'est pas un JSON exploitable.</summary>
    JsonInvalide,

    /// <summary>Le JSON est valide mais ne décrit aucun produit.</summary>
    LigneVide,

    /// <summary>Code-barres ou nom absent — le catalogue ne saurait pas désigner ce produit.</summary>
    IdentiteAbsente,

    /// <summary>Une valeur nutritionnelle négative — le produit est écarté plutôt que faussé.</summary>
    ValeurAberrante
}

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
    /// <param name="rejet">
    /// Motif du rejet, ou <see cref="OffRejection.Aucun"/> si la ligne a été convertie. Remonté à
    /// l'appelant plutôt que journalisé ici : cette classe est statique et n'a donc pas de
    /// journaliseur, et l'y introduire lui ferait porter une responsabilité qui n'est pas la sienne.
    /// </param>
    /// <returns>Le produit normalisé, ou <c>null</c> s'il doit être ignoré.</returns>
    public static OffProduct? TryMap(string jsonLine, out OffRejection rejet)
    {
        RawProduct? raw;
        try
        {
            raw = JsonSerializer.Deserialize<RawProduct>(jsonLine, Options);
        }
        catch (JsonException)
        {
            rejet = OffRejection.JsonInvalide;
            return null;
        }

        if (raw is null)
        {
            rejet = OffRejection.LigneVide;
            return null;
        }

        var offId = raw.Code?.Trim();
        var name = raw.ProductName?.Trim();

        // Un produit sans code-barres ou sans nom n'est pas exploitable par le catalogue.
        if (string.IsNullOrWhiteSpace(offId) || string.IsNullOrWhiteSpace(name))
        {
            rejet = OffRejection.IdentiteAbsente;
            return null;
        }

        var calories = raw.Nutriments?.EnergyKcal ?? 0f;
        var proteins = ToInt(raw.Nutriments?.Proteins);
        var carbs = ToInt(raw.Nutriments?.Carbs);
        var fats = ToInt(raw.Nutriments?.Fat);

        // Valeurs aberrantes (négatives) → produit ignoré plutôt que faussé.
        if (calories < 0 || proteins < 0 || carbs < 0 || fats < 0)
        {
            rejet = OffRejection.ValeurAberrante;
            return null;
        }

        rejet = OffRejection.Aucun;

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
