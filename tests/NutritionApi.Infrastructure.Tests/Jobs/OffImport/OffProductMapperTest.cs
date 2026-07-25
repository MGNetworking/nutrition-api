namespace NutritionApi.Infrastructure.Tests.Jobs.OffImport;

using NutritionApi.Domain.Enums;
using NutritionApi.Infrastructure.Jobs.OffImport;

public class OffProductMapperTest
{
    // ---------------------------------------------------------------------
    // Chemin nominal
    // ---------------------------------------------------------------------

    [Fact]
    public void TryMap_WhenValidProduct_MapsAllFields()
    {
        const string json = """
            {"code":"3017620422003","product_name":"Nutella",
             "allergens_tags":["en:milk","en:nuts","en:soybeans"],
             "nutriments":{"energy-kcal_100g":539,"proteins_100g":6.3,"carbohydrates_100g":57.5,"fat_100g":30.9}}
            """;

        var product = OffProductMapper.TryMap(json);

        Assert.NotNull(product);
        Assert.Equal("3017620422003", product!.OffId);
        Assert.Equal("Nutella", product.Name);
        Assert.Equal(539f, product.CaloriesPer100g);
        Assert.Equal(6, product.ProteinsPer100g);    // 6.3 → 6
        Assert.Equal(58, product.CarbsPer100g);       // 57.5 → 58
        Assert.Equal(31, product.FatsPer100g);        // 30.9 → 31
        Assert.Equal(new[] { Allergen.Milk, Allergen.Nuts, Allergen.Soybeans }, product.AllergensTags);
    }

    // ---------------------------------------------------------------------
    // Arrondi des valeurs nutritionnelles (au plus proche, 0.5 → sup)
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("3.2", 3)]
    [InlineData("3.5", 4)]
    [InlineData("3.6", 4)]
    [InlineData("12.5", 13)]
    [InlineData("0", 0)]
    public void TryMap_RoundsMacrosToNearestInt(string rawProteins, int expected)
    {
        var json = "{\"code\":\"1\",\"product_name\":\"P\",\"nutriments\":{\"proteins_100g\":" + rawProteins + "}}";

        var product = OffProductMapper.TryMap(json);

        Assert.NotNull(product);
        Assert.Equal(expected, product!.ProteinsPer100g);
    }

    // ---------------------------------------------------------------------
    // Allergènes
    // ---------------------------------------------------------------------

    [Fact]
    public void TryMap_MapsKnownAllergens_AndIgnoresUnknown()
    {
        const string json = """
            {"code":"1","product_name":"P","allergens_tags":["en:milk","en:unknown-xyz","en:gluten"]}
            """;

        var product = OffProductMapper.TryMap(json);

        Assert.NotNull(product);
        Assert.Equal(new[] { Allergen.Milk, Allergen.Gluten }, product!.AllergensTags);
        Assert.DoesNotContain(Allergen.Unknown, product.AllergensTags);
    }

    [Fact]
    public void TryMap_DeduplicatesAllergens()
    {
        const string json = """
            {"code":"1","product_name":"P","allergens_tags":["en:milk","en:milk"]}
            """;

        var product = OffProductMapper.TryMap(json);

        Assert.NotNull(product);
        Assert.Equal(new[] { Allergen.Milk }, product!.AllergensTags);
    }

    [Fact]
    public void TryMap_WhenNoAllergensTags_ReturnsEmptyList()
    {
        const string json = """{"code":"1","product_name":"P"}""";

        var product = OffProductMapper.TryMap(json);

        Assert.NotNull(product);
        Assert.Empty(product!.AllergensTags);
    }

    // ---------------------------------------------------------------------
    // Champs manquants / valeurs par défaut
    // ---------------------------------------------------------------------

    [Fact]
    public void TryMap_WhenNutrimentsMissing_DefaultsToZero()
    {
        const string json = """{"code":"1","product_name":"P"}""";

        var product = OffProductMapper.TryMap(json);

        Assert.NotNull(product);
        Assert.Equal(0f, product!.CaloriesPer100g);
        Assert.Equal(0, product.ProteinsPer100g);
        Assert.Equal(0, product.CarbsPer100g);
        Assert.Equal(0, product.FatsPer100g);
    }

    [Fact]
    public void TryMap_WhenNutrimentsAsStrings_ParsesThem()
    {
        const string json = """{"code":"1","product_name":"P","nutriments":{"proteins_100g":"7.4"}}""";

        var product = OffProductMapper.TryMap(json);

        Assert.NotNull(product);
        Assert.Equal(7, product!.ProteinsPer100g);
    }

    // ---------------------------------------------------------------------
    // Produits ignorés (retour null)
    // ---------------------------------------------------------------------

    [Fact]
    public void TryMap_WhenCodeMissing_ReturnsNull()
    {
        const string json = """{"product_name":"P","nutriments":{}}""";
        Assert.Null(OffProductMapper.TryMap(json));
    }

    [Theory]
    [InlineData("""{"code":"1","nutriments":{}}""")]
    [InlineData("""{"code":"1","product_name":"","nutriments":{}}""")]
    [InlineData("""{"code":"1","product_name":"   "}""")]
    public void TryMap_WhenNameMissingOrBlank_ReturnsNull(string json)
    {
        Assert.Null(OffProductMapper.TryMap(json));
    }

    [Fact]
    public void TryMap_WhenNegativeNutriment_ReturnsNull()
    {
        const string json = """{"code":"1","product_name":"P","nutriments":{"proteins_100g":-5}}""";
        Assert.Null(OffProductMapper.TryMap(json));
    }

    [Theory]
    [InlineData("{ ceci n'est pas du json")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryMap_WhenInvalidJson_ReturnsNull(string json)
    {
        Assert.Null(OffProductMapper.TryMap(json));
    }
}
