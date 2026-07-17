namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.Enums;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

public class NutritionServiceTest
{
    private readonly Mock<IDietRepository> _dietRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _userRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IMealRepository> _mealRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IWeightEntryRepository> _weightEntryRepositoryMock = new(MockBehavior.Strict);
    private readonly SubscriptionGuard _subscriptionGuard = new();
    private readonly NutritionService _nutritionService;

    public NutritionServiceTest()
    {
        _nutritionService = new NutritionService(
            _dietRepositoryMock.Object,
            _subscriptionGuard,
            _userRepositoryMock.Object,
            _mealRepositoryMock.Object,
            _weightEntryRepositoryMock.Object);
    }

    private static User BuildUser(SubscriptionTier tier = SubscriptionTier.Free)
    {
        var user = new User(
            "keycloak-123",
            new DateOnly(1990, 1, 1),
            Gender.Male,
            ActivityLevel.ModeratelyActive,
            180f,
            [],
            []);

        if (tier != SubscriptionTier.Free)
            user.ChangeSubscriptionTier(tier);

        return user;
    }

    private static Diet BuildDiet(Guid userId) =>
        new(userId, "Test Diet", DietType.Balanced, Goal.WeightLoss, 75f, 2000, new MacroDistribution(30, 40, 30));

    private static Meal BuildMeal(Guid userId, DateTime consumedAt, params NutritionInfo[] items)
    {
        var mealItems = items
            .Select(n => new MealItem(Guid.NewGuid(), Guid.NewGuid(), 100f, n))
            .ToList();

        return new Meal(userId, "Repas test", MealType.Lunch, null, mealItems, consumedAt, false);
    }

    private static WeightEntry BuildWeightEntry(Guid userId, DateOnly measuredAt, float weight = 80f) =>
        new(userId, weight, measuredAt);

    // --- GetBilanAsync — chemin nominal ---

    [Fact]
    public async Task GetBilanAsync_ShouldReturnBilan_WhenDietIsActive()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);
        var meal = BuildMeal(user.Id, diet.StartDate.ToDateTime(new TimeOnly(12, 0)), new NutritionInfo(500f, 30, 50, 15));
        var weightEntry = BuildWeightEntry(user.Id, diet.StartDate);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([meal]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([weightEntry]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, null, null, null, null);

        Assert.Equal(diet.Id, result.DietId);
        Assert.Single(result.DailyBreakdown);
        Assert.Equal(500f, result.TotalCalories);
        Assert.Single(result.WeightProgression);
    }

    [Fact]
    public async Task GetBilanAsync_ShouldReturnBilan_WhenDietIsArchived()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);
        diet.ChangeDietStatus(DietStatus.Archived);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, null, null, null, null);

        Assert.Equal(diet.EndDate, result.EndDate);
    }

    [Fact]
    public async Task GetBilanAsync_ShouldMergeMealsOfSameDay_WhenComputingDailyBreakdown()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);
        var breakfast = BuildMeal(user.Id, diet.StartDate.ToDateTime(new TimeOnly(8, 0)), new NutritionInfo(300f, 20, 30, 10));
        var dinner = BuildMeal(user.Id, diet.StartDate.ToDateTime(new TimeOnly(20, 0)), new NutritionInfo(500f, 30, 50, 15));

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([breakfast, dinner]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, null, null, null, null);

        var day = Assert.Single(result.DailyBreakdown);
        Assert.Equal(diet.StartDate, day.Date);
        Assert.Equal(800f, day.Calories);
        Assert.Equal(800f, result.TotalCalories);
    }

    [Fact]
    public async Task GetBilanAsync_ShouldIncludeWeightProgression_WhenEntriesExistInPeriod()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);
        var weightEntry = BuildWeightEntry(user.Id, diet.StartDate, 82.5f);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([weightEntry]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, null, null, null, null);

        var entry = Assert.Single(result.WeightProgression);
        Assert.Equal(diet.StartDate, entry.Date);
        Assert.Equal(82.5f, entry.Weight);
    }

    // --- GetBilanAsync — cas limites ---

    [Fact]
    public async Task GetBilanAsync_ShouldReturnEmptyDailyBreakdown_WhenNoMealsInPeriod()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, null, null, null, null);

        Assert.Empty(result.DailyBreakdown);
        Assert.Equal(0f, result.TotalCalories);
    }

    [Fact]
    public async Task GetBilanAsync_ShouldReturnEmptyWeightProgression_WhenNoWeightEntriesInPeriod()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, null, null, null, null);

        Assert.Empty(result.WeightProgression);
    }

    [Fact]
    public async Task GetBilanAsync_ShouldClampPeriodToDietWindow_WhenRequestedPeriodExceedsDietDuration()
    {
        var user = BuildUser(SubscriptionTier.Business);
        var diet = BuildDiet(user.Id);
        var farPast = diet.StartDate.AddDays(-100);
        var farFuture = diet.StartDate.AddDays(100);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, BilanPeriod.Custom, null, farPast, farFuture);

        Assert.Equal(diet.StartDate, result.StartDate);
        Assert.Equal(diet.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow), result.EndDate);
    }

    [Fact]
    public async Task GetBilanAsync_ShouldUseTodayAsEndDate_WhenDietIsStillActive()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        var result = await _nutritionService.GetBilanAsync(user.Id, diet.Id, null, null, null, null);

        Assert.Null(diet.EndDate);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), result.EndDate);
    }

    // --- GetBilanAsync — cas d'erreur ---

    [Fact]
    public async Task GetBilanAsync_ShouldThrow_WhenDietNotFound()
    {
        var userId = Guid.NewGuid();
        var dietId = Guid.NewGuid();

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(dietId)).ReturnsAsync((Diet?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _nutritionService.GetBilanAsync(userId, dietId, null, null, null, null));
    }

    [Fact]
    public async Task GetBilanAsync_ShouldThrow_WhenDietDoesNotBelongToUser()
    {
        var owner = BuildUser();
        var diet = BuildDiet(owner.Id);
        var otherUserId = Guid.NewGuid();

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _nutritionService.GetBilanAsync(otherUserId, diet.Id, null, null, null, null));
    }

    [Fact]
    public async Task GetBilanAsync_ShouldThrow_WhenPeriodExceedsTierLimit()
    {
        var user = BuildUser(SubscriptionTier.Free);
        var diet = BuildDiet(user.Id);
        var startDate = diet.StartDate.AddDays(-30);
        var endDate = diet.StartDate;

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _nutritionService.GetBilanAsync(user.Id, diet.Id, BilanPeriod.Custom, null, startDate, endDate));
    }

    [Fact]
    public async Task GetBilanAsync_ShouldThrow_WhenPeriodParametersAreInvalid()
    {
        var user = BuildUser();
        var diet = BuildDiet(user.Id);

        _dietRepositoryMock.Setup(r => r.GetByIdAsync(diet.Id)).ReturnsAsync(diet);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await Assert.ThrowsAsync<UnprocessableException>(
            () => _nutritionService.GetBilanAsync(user.Id, diet.Id, BilanPeriod.Custom, null, null, null));
    }
}
