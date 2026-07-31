using NutritionApi.Domain.Entity;

namespace NutritionApi.Domain.Tests;

[Trait("Level", "1")]
public class SavedFoodItemTest
{
    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        Guid userId = Guid.NewGuid();
        Guid foodItemId = Guid.NewGuid();

        SavedFoodItem item = new SavedFoodItem(userId, foodItemId);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(userId, item.UserId);
        Assert.Equal(foodItemId, item.FoodItemId);
        Assert.True(item.SavedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_UserId_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new SavedFoodItem(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_FoodItemId_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new SavedFoodItem(Guid.NewGuid(), Guid.Empty));
    }
}
