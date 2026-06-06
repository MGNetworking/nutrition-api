using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Tests;

public class NutritionNeedsTest
{
    static MacroDistribution DefaultMacros() => new(20, 50, 30);

    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        MacroDistribution macros = DefaultMacros();
        NutritionNeeds needs = new(1500.0f, 2000.0f, 1800.0f, macros);

        Assert.Equal(1500.0f, needs.Bmr);
        Assert.Equal(2000.0f, needs.Tdee);
        Assert.Equal(1800.0f, needs.TargetCalories);
        Assert.Equal(macros, needs.MacroDistribution);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void Constructor_Bmr_Invalid_ThrowsArgumentOutOfRangeExceptionTest(float bmr)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionNeeds(bmr, 2000.0f, 1800.0f, DefaultMacros()));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void Constructor_Tdee_Invalid_ThrowsArgumentOutOfRangeExceptionTest(float tdee)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionNeeds(1500.0f, tdee, 1800.0f, DefaultMacros()));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void Constructor_TargetCalories_Invalid_ThrowsArgumentOutOfRangeExceptionTest(float targetCalories)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionNeeds(1500.0f, 2000.0f, targetCalories, DefaultMacros()));
    }

    [Fact]
    public void Constructor_MacroDistribution_Null_ThrowsArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new NutritionNeeds(1500.0f, 2000.0f, 1800.0f, null!));
    }

    [Fact]
    public void Constructor_SameValues_AreEqual_OkTest()
    {
        NutritionNeeds a = new(1500.0f, 2000.0f, 1800.0f, DefaultMacros());
        NutritionNeeds b = new(1500.0f, 2000.0f, 1800.0f, DefaultMacros());

        Assert.Equal(a, b);
    }

    [Fact]
    public void Constructor_DifferentValues_AreNotEqual_OkTest()
    {
        NutritionNeeds a = new(1500.0f, 2000.0f, 1800.0f, DefaultMacros());
        NutritionNeeds b = new(1600.0f, 2100.0f, 1900.0f, DefaultMacros());

        Assert.NotEqual(a, b);
    }
}
