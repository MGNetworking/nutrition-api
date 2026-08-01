using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Tests;

[Trait("Level", "1")]
public class MacroDistributionTest
{
    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        MacroDistribution macros = new(20, 50, 30);

        Assert.Equal(20, macros.ProteinPercentage);
        Assert.Equal(50, macros.CarbPercentage);
        Assert.Equal(30, macros.FatPercentage);
    }

    [Theory]
    [InlineData(0, 50, 50)]
    [InlineData(-1, 50, 51)]
    public void Constructor_Protein_Invalid_ThrowsArgumentOutOfRangeExceptionTest(int protein, int carb, int fat)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MacroDistribution(protein, carb, fat));
    }

    [Theory]
    [InlineData(20, 0, 80)]
    [InlineData(20, -1, 81)]
    public void Constructor_Carb_Invalid_ThrowsArgumentOutOfRangeExceptionTest(int protein, int carb, int fat)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MacroDistribution(protein, carb, fat));
    }

    [Theory]
    [InlineData(50, 50, 0)]
    [InlineData(50, 51, -1)]
    public void Constructor_Fat_Invalid_ThrowsArgumentOutOfRangeExceptionTest(int protein, int carb, int fat)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new MacroDistribution(protein, carb, fat));
    }

    [Theory]
    [InlineData(20, 50, 29)]
    [InlineData(20, 50, 31)]
    [InlineData(10, 10, 10)]
    public void Constructor_SumNot100_ThrowsArgumentExceptionTest(int protein, int carb, int fat)
    {
        Assert.Throws<ArgumentException>(() => new MacroDistribution(protein, carb, fat));
    }

    [Fact]
    public void Constructor_SameValues_AreEqual_OkTest()
    {
        MacroDistribution a = new(20, 50, 30);
        MacroDistribution b = new(20, 50, 30);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Constructor_DifferentValues_AreNotEqual_OkTest()
    {
        MacroDistribution a = new(20, 50, 30);
        MacroDistribution b = new(30, 40, 30);

        Assert.NotEqual(a, b);
    }
}
