using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Tests;

public class NutritionInfoTest
{
    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        NutritionInfo info = new(200.0f, 20, 15, 8);

        Assert.Equal(200.0f, info.Calories);
        Assert.Equal(20, info.Proteins);
        Assert.Equal(15, info.Carbs);
        Assert.Equal(8, info.Fats);
    }

    [Fact]
    public void Constructor_Calories_Zero_OkTest()
    {
        NutritionInfo info = new(0.0f, 0, 0, 0);
        Assert.Equal(0.0f, info.Calories);
    }

    [Fact]
    public void Constructor_Calories_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionInfo(-1.0f, 20, 15, 8));
    }

    [Fact]
    public void Constructor_Proteins_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionInfo(200.0f, -1, 15, 8));
    }

    [Fact]
    public void Constructor_Carbs_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionInfo(200.0f, 20, -1, 8));
    }

    [Fact]
    public void Constructor_Fats_Negative_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NutritionInfo(200.0f, 20, 15, -1));
    }

    [Fact]
    public void Constructor_SameValues_AreEqual_OkTest()
    {
        NutritionInfo a = new(200.0f, 20, 15, 8);
        NutritionInfo b = new(200.0f, 20, 15, 8);

        Assert.Equal(a, b);
    }

    [Fact]
    public void Constructor_DifferentValues_AreNotEqual_OkTest()
    {
        NutritionInfo a = new(200.0f, 20, 15, 8);
        NutritionInfo b = new(300.0f, 25, 10, 5);

        Assert.NotEqual(a, b);
    }
}
