namespace NutritionApi.Domain.Entity;

using NutritionApi.Domain.Enums;

public class Meal
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Name { get; private set; } = string.Empty;
    public MealType MealType { get; private set; } = MealType.Unknown;
    public string? Notes { get; private set; }
    public List<MealItem> MealItems { get; private set; } = new List<MealItem>();
    public DateTime ConsumedAt { get; private set; }
    public bool IsSaved { get; private set; }

    // Pour EF Core uniquement 
    private Meal() { }

    public Meal(
        Guid userId,
        string name,
        MealType mealType,
        string? notes,
        List<MealItem> mealItems,
        DateTime consumedAt,
        bool isSaved)
    {
        Id = Guid.NewGuid();
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Received: {userId}", nameof(userId));


        if (consumedAt == default)
            throw new ArgumentException($"Consumed date must be defined. Received: {consumedAt}", nameof(consumedAt));
        

        if (mealItems == null || mealItems.Count == 0)
            throw new ArgumentException("Meal must contain at least one MealItem.", nameof(mealItems));

        
        UserId = userId;
        ConsumedAt = consumedAt;
        MealItems = mealItems;
        IsSaved = isSaved;

        Rename(name);
        ChangeNote(notes);
        ChangeMealType(mealType);
    }

    public void AddMealItem(MealItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        MealItems.Add(item);
    }

    public void RemoveMealItem(Guid mealItemId)
    {
        if (MealItems.Count <= 1)
            throw new InvalidOperationException("Meal must contain at least one MealItem.");
        var item = MealItems.FirstOrDefault(x => x.Id == mealItemId)
            ?? throw new ArgumentException($"MealItem not found. Received: {mealItemId}", nameof(mealItemId));
        MealItems.Remove(item);
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    public void ChangeNote(string? note)
    {
        this.Notes = note; // null = suppression volontaire de la note
    }

    public void ChangeMealType(MealType mealType)
    {
        if (mealType == MealType.Unknown)
            throw new ArgumentException($"Meal type must be defined. Received: {mealType}", nameof(mealType));
        MealType = mealType;
    }

    public void ChangeConsumedAt(DateTime consumedAt)
    {
        if (consumedAt == default)
            throw new ArgumentException($"Consumed date must be defined. Received: {consumedAt}", nameof(consumedAt));
        ConsumedAt = consumedAt;
    }

}
