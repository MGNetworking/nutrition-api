using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Domain.Tests;

[Trait("Level", "1")]
public class FoodItemTest
{
    static FoodItem CreateFoodItem(
        string offId = "off-123",
        string name = "Poulet rôti",
        float calories = 165.0f,
        int proteins = 31,
        int carbs = 0,
        int fats = 4,
        List<Allergen>? allergens = null)
    {
        return new FoodItem(
            offId: offId,
            name: name,
            caloriesPer100g: calories,
            proteinsPer100g: proteins,
            carbsPer100g: carbs,
            fatsPer100g: fats,
            allergensTags: allergens ?? new List<Allergen>());
    }

    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        var allergens = new List<Allergen> { Allergen.Gluten };
        FoodItem item = CreateFoodItem(allergens: allergens);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal("off-123", item.OffId);
        Assert.Equal("Poulet rôti", item.Name);
        Assert.Equal(165.0f, item.CaloriesPer100g);
        Assert.Equal(31, item.ProteinsPer100g);
        Assert.Equal(0, item.CarbsPer100g);
        Assert.Equal(4, item.FatsPer100g);
        Assert.Equal(allergens, item.AllergensTags);
        Assert.True(item.CachedAt <= DateTime.UtcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_OffId_Invalid_ThrowsArgumentExceptionTest(string? offId)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateFoodItem(offId: offId!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Name_Invalid_ThrowsArgumentExceptionTest(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateFoodItem(name: name!));
    }

    [Fact]
    public void Constructor_Calories_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateFoodItem(calories: -1.0f));
    }

    [Fact]
    public void Constructor_Calories_Zero_OkTest()
    {
        FoodItem item = CreateFoodItem(calories: 0.0f);
        Assert.Equal(0.0f, item.CaloriesPer100g);
    }

    [Fact]
    public void Constructor_Proteins_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateFoodItem(proteins: -1));
    }

    [Fact]
    public void Constructor_Carbs_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateFoodItem(carbs: -1));
    }

    [Fact]
    public void Constructor_Fats_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateFoodItem(fats: -1));
    }

    [Fact]
    public void Constructor_AllergensTags_Null_ThrowsArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new FoodItem(
            offId: "off-123",
            name: "Poulet rôti",
            caloriesPer100g: 165.0f,
            proteinsPer100g: 31,
            carbsPer100g: 0,
            fatsPer100g: 4,
            allergensTags: null!));
    }

    // --- UpdateFromImport ---

    [Fact]
    public void UpdateFromImport_OkTest()
    {
        FoodItem item = CreateFoodItem();
        DateTime cachedAtBefore = item.CachedAt;
        var newAllergens = new List<Allergen> { Allergen.Milk };

        item.UpdateFromImport(
            name: "Poulet grillé",
            caloriesPer100g: 180.0f,
            proteinsPer100g: 35,
            carbsPer100g: 1,
            fatsPer100g: 5,
            allergensTags: newAllergens);

        Assert.Equal("Poulet grillé", item.Name);
        Assert.Equal(180.0f, item.CaloriesPer100g);
        Assert.Equal(35, item.ProteinsPer100g);
        Assert.Equal(1, item.CarbsPer100g);
        Assert.Equal(5, item.FatsPer100g);
        Assert.Equal(newAllergens, item.AllergensTags);
        Assert.True(item.CachedAt >= cachedAtBefore);
    }

    [Fact]
    public void UpdateFromImport_OffId_NotChanged_OkTest()
    {
        FoodItem item = CreateFoodItem(offId: "off-abc");
        item.UpdateFromImport("Nouveau nom", 100.0f, 10, 10, 5, new List<Allergen>());
        Assert.Equal("off-abc", item.OffId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void UpdateFromImport_Name_Invalid_ThrowsArgumentExceptionTest(string? name)
    {
        FoodItem item = CreateFoodItem();
        Assert.ThrowsAny<ArgumentException>(() =>
            item.UpdateFromImport(name!, 165.0f, 31, 0, 4, new List<Allergen>()));
    }

    [Fact]
    public void UpdateFromImport_Calories_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        FoodItem item = CreateFoodItem();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            item.UpdateFromImport("Poulet", -1.0f, 31, 0, 4, new List<Allergen>()));
    }

    [Fact]
    public void UpdateFromImport_Proteins_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        FoodItem item = CreateFoodItem();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            item.UpdateFromImport("Poulet", 165.0f, -1, 0, 4, new List<Allergen>()));
    }

    [Fact]
    public void UpdateFromImport_Carbs_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        FoodItem item = CreateFoodItem();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            item.UpdateFromImport("Poulet", 165.0f, 31, -1, 4, new List<Allergen>()));
    }

    [Fact]
    public void UpdateFromImport_Fats_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        FoodItem item = CreateFoodItem();
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            item.UpdateFromImport("Poulet", 165.0f, 31, 0, -1, new List<Allergen>()));
    }

    [Fact]
    public void UpdateFromImport_AllergensTags_Null_ThrowsArgumentNullExceptionTest()
    {
        FoodItem item = CreateFoodItem();
        Assert.Throws<ArgumentNullException>(() =>
            item.UpdateFromImport("Poulet", 165.0f, 31, 0, 4, null!));
    }
}
