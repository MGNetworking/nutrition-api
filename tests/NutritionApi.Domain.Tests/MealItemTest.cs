using NutritionApi.Domain.Entity;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Tests;

public class MealItemTest
{
    static NutritionInfo DefaultNutrition() => new(200.0f, 20, 15, 8);

    static MealItem CreateMealItem(
        Guid? mealId = null,
        Guid? foodItemId = null,
        float quantity = 150.0f,
        NutritionInfo? nutrition = null)
    {
        return new MealItem(
            mealId: mealId ?? Guid.NewGuid(),
            foodItemId: foodItemId ?? Guid.NewGuid(),
            quantity: quantity,
            nutrition: nutrition ?? DefaultNutrition());
    }

    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        Guid mealId = Guid.NewGuid();
        Guid foodItemId = Guid.NewGuid();
        NutritionInfo nutrition = DefaultNutrition();

        MealItem item = new MealItem(mealId, foodItemId, 150.0f, nutrition);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(mealId, item.MealId);
        Assert.Equal(foodItemId, item.FoodItemId);
        Assert.Equal(150.0f, item.Quantity);
        Assert.Equal(nutrition, item.Nutrition);
    }

    [Fact]
    public void Constructor_MealId_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateMealItem(mealId: Guid.Empty));
    }

    [Fact]
    public void Constructor_FoodItemId_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateMealItem(foodItemId: Guid.Empty));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void Constructor_Quantity_Invalid_ThrowsArgumentOutOfRangeExceptionTest(float quantity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateMealItem(quantity: quantity));
    }

    [Fact]
    public void Constructor_Nutrition_Null_ThrowsArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new MealItem(
            mealId: Guid.NewGuid(),
            foodItemId: Guid.NewGuid(),
            quantity: 150.0f,
            nutrition: null!));
    }
}
