namespace NutritionApi.Domain.Entity;

using ValueObjects;

public class MealItem
{
    public Guid Id { get; init; }
    public Guid MealId { get; init; }
    public Guid FoodItemId { get; init; }
    public float Quantity { get; private set; }
    public NutritionInfo Nutrition { get; private set; } = null!;
    public FoodItem? FoodItem { get; init; }

    // Pour EF Core uniquement
    private MealItem() { }

    public MealItem(Guid mealId, Guid foodItemId, float quantity, NutritionInfo nutrition)
    {
        Id = Guid.NewGuid();

        if (mealId == Guid.Empty)
            throw new ArgumentException($"Meal ID cannot be empty. Received: {mealId}", nameof(mealId));
        if (foodItemId == Guid.Empty)
            throw new ArgumentException($"Food item ID cannot be empty. Received: {foodItemId}", nameof(foodItemId));

        MealId = mealId;
        FoodItemId = foodItemId;

        SetQuantity(quantity);
        SetNutrition(nutrition);
    }

    private void SetQuantity(float quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Quantity = quantity;
    }

    private void SetNutrition(NutritionInfo nutrition)
    {
        ArgumentNullException.ThrowIfNull(nutrition);
        Nutrition = nutrition;
    }

}
