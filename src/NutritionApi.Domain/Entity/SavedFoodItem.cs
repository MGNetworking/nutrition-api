namespace NutritionApi.Domain.Entity;

public class SavedFoodItem
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid FoodItemId { get; init; }
    public DateTime SavedAt { get; init; }

    // Pour EF Core uniquement
    private SavedFoodItem() { }

    public SavedFoodItem(Guid userId, Guid foodItemId)
    {
        Id = Guid.NewGuid();

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        UserId = userId;

        if (foodItemId == Guid.Empty)
            throw new ArgumentException("foodItemId cannot be empty.", nameof(foodItemId));
        FoodItemId = foodItemId;

        SavedAt = DateTime.UtcNow;
    }
}
