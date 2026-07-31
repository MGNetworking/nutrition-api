using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Tests;

[Trait("Level", "1")]
public class MealTest
{
    static NutritionInfo DefaultNutrition() => new(200.0f, 20, 15, 8);

    static MealItem CreateMealItem(Guid? mealId = null, Guid? foodItemId = null)
    {
        return new MealItem(
            mealId: mealId ?? Guid.NewGuid(),
            foodItemId: foodItemId ?? Guid.NewGuid(),
            quantity: 150.0f,
            nutrition: DefaultNutrition());
    }

    static Meal CreateMeal(
        Guid? userId = null,
        string name = "Déjeuner",
        MealType mealType = MealType.Lunch,
        string? notes = null,
        List<MealItem>? mealItems = null,
        DateTime? consumedAt = null,
        bool isSaved = false)
    {
        return new Meal(
            userId: userId ?? Guid.NewGuid(),
            name: name,
            mealType: mealType,
            notes: notes,
            mealItems: mealItems ?? new List<MealItem> { CreateMealItem() },
            consumedAt: consumedAt ?? DateTime.UtcNow,
            isSaved: isSaved);
    }

    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        Guid userId = Guid.NewGuid();
        DateTime consumedAt = DateTime.UtcNow;
        var items = new List<MealItem> { CreateMealItem() };

        Meal meal = new Meal(userId, "Déjeuner", MealType.Lunch, "une note", items, consumedAt, false);

        Assert.NotEqual(Guid.Empty, meal.Id);
        Assert.Equal(userId, meal.UserId);
        Assert.Equal("Déjeuner", meal.Name);
        Assert.Equal(MealType.Lunch, meal.MealType);
        Assert.Equal("une note", meal.Notes);
        Assert.Equal(consumedAt, meal.ConsumedAt);
        Assert.Single(meal.MealItems);
        Assert.False(meal.IsSaved);
    }

    [Fact]
    public void Constructor_CreatedAt_IsSetToUtcNowTest()
    {
        var before = DateTime.UtcNow;

        var meal = CreateMeal();

        var after = DateTime.UtcNow;
        Assert.InRange(meal.CreatedAt, before, after);
    }

    [Fact]
    public void Constructor_UserId_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateMeal(userId: Guid.Empty));
    }

    [Fact]
    public void Constructor_ConsumedAt_Default_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new Meal(
            userId: Guid.NewGuid(),
            name: "Déjeuner",
            mealType: MealType.Lunch,
            notes: null,
            mealItems: new List<MealItem> { CreateMealItem() },
            consumedAt: default(DateTime),
            isSaved: false));
    }

    [Fact]
    public void Constructor_MealItems_Null_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new Meal(
            userId: Guid.NewGuid(),
            name: "Déjeuner",
            mealType: MealType.Lunch,
            notes: null,
            mealItems: null!,
            consumedAt: DateTime.UtcNow,
            isSaved: false));
    }

    [Fact]
    public void Constructor_MealItems_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateMeal(mealItems: new List<MealItem>()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Name_Invalid_ThrowsArgumentExceptionTest(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => CreateMeal(name: name!));
    }

    [Fact]
    public void Constructor_MealType_Unknown_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateMeal(mealType: MealType.Unknown));
    }

    [Fact]
    public void Constructor_Notes_Null_OkTest()
    {
        Meal meal = CreateMeal(notes: null);
        Assert.Null(meal.Notes);
    }

    // --- AddMealItem ---

    [Fact]
    public void AddMealItem_Null_ThrowsArgumentNullExceptionTest()
    {
        Meal meal = CreateMeal();
        Assert.Throws<ArgumentNullException>(() => meal.AddMealItem(null!));
    }

    [Fact]
    public void AddMealItem_OkTest()
    {
        Meal meal = CreateMeal();
        int countBefore = meal.MealItems.Count;
        meal.AddMealItem(CreateMealItem());
        Assert.Equal(countBefore + 1, meal.MealItems.Count);
    }

    // --- RemoveMealItem ---

    [Fact]
    public void RemoveMealItem_LastItem_ThrowsInvalidOperationExceptionTest()
    {
        Meal meal = CreateMeal();
        Assert.Single(meal.MealItems);
        Assert.Throws<InvalidOperationException>(() => meal.RemoveMealItem(meal.MealItems[0].Id));
    }

    [Fact]
    public void RemoveMealItem_NotFound_ThrowsArgumentExceptionTest()
    {
        Meal meal = CreateMeal();
        meal.AddMealItem(CreateMealItem());
        Assert.Throws<ArgumentException>(() => meal.RemoveMealItem(Guid.NewGuid()));
    }

    [Fact]
    public void RemoveMealItem_OkTest()
    {
        Meal meal = CreateMeal();
        MealItem second = CreateMealItem();
        meal.AddMealItem(second);
        meal.RemoveMealItem(second.Id);
        Assert.Single(meal.MealItems);
        Assert.DoesNotContain(second, meal.MealItems);
    }

    // --- Rename ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_Invalid_ThrowsArgumentExceptionTest(string? name)
    {
        Meal meal = CreateMeal();
        Assert.ThrowsAny<ArgumentException>(() => meal.Rename(name!));
    }

    [Fact]
    public void Rename_OkTest()
    {
        Meal meal = CreateMeal();
        meal.Rename("Dîner");
        Assert.Equal("Dîner", meal.Name);
    }

    // --- ChangeNote ---

    [Fact]
    public void ChangeNote_Null_OkTest()
    {
        Meal meal = CreateMeal(notes: "ancienne note");
        meal.ChangeNote(null);
        Assert.Null(meal.Notes);
    }

    [Fact]
    public void ChangeNote_OkTest()
    {
        Meal meal = CreateMeal();
        meal.ChangeNote("nouvelle note");
        Assert.Equal("nouvelle note", meal.Notes);
    }

    // --- ChangeMealType ---

    [Fact]
    public void ChangeMealType_Unknown_ThrowsArgumentExceptionTest()
    {
        Meal meal = CreateMeal();
        Assert.Throws<ArgumentException>(() => meal.ChangeMealType(MealType.Unknown));
    }

    [Fact]
    public void ChangeMealType_OkTest()
    {
        Meal meal = CreateMeal();
        meal.ChangeMealType(MealType.Dinner);
        Assert.Equal(MealType.Dinner, meal.MealType);
    }

    // --- ChangeConsumedAt ---

    [Fact]
    public void ChangeConsumedAt_Default_ThrowsArgumentExceptionTest()
    {
        Meal meal = CreateMeal();
        Assert.Throws<ArgumentException>(() => meal.ChangeConsumedAt(default));
    }

    [Fact]
    public void ChangeConsumedAt_OkTest()
    {
        Meal meal = CreateMeal();
        DateTime newDate = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        meal.ChangeConsumedAt(newDate);
        Assert.Equal(newDate, meal.ConsumedAt);
    }
}
