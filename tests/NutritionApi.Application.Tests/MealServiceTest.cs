namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

[Trait("Level", "1")]
public class MealServiceTest
{
    private readonly Mock<IMealRepository> _mealRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _userRepositoryMock = new(MockBehavior.Strict);
    private readonly SubscriptionGuard _subscriptionGuard = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly MealService _mealService;

    public MealServiceTest()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        _mealService = new MealService(
            _mealRepositoryMock.Object,
            _foodItemRepositoryMock.Object,
            _userRepositoryMock.Object,
            _subscriptionGuard,
            _unitOfWorkMock.Object);
    }

    private static FoodItem BuildFoodItem()
        => new("off-001", "Poulet", 165f, 31, 0, 4, []);

    private static User BuildUser(string keycloakId = "keycloak-123")
        => new(keycloakId, new DateOnly(1990, 1, 1), Gender.Male, ActivityLevel.ModeratelyActive, 180f, [], []);

    private static Meal BuildMeal(Guid userId, FoodItem foodItem)
    {
        var nutrition = new NutritionInfo(247.5f, 46, 0, 6);
        var item = new MealItem(Guid.NewGuid(), foodItem.Id, 150f, nutrition) { FoodItem = foodItem };
        return new Meal(userId, "Déjeuner", MealType.Lunch, null, [item], DateTime.UtcNow, false);
    }

    private static Meal BuildMealWithTwoItems(Guid userId, FoodItem foodItem)
    {
        var nutrition = new NutritionInfo(247.5f, 46, 0, 6);
        var item1 = new MealItem(Guid.NewGuid(), foodItem.Id, 150f, nutrition) { FoodItem = foodItem };
        var item2 = new MealItem(Guid.NewGuid(), foodItem.Id, 100f, nutrition) { FoodItem = foodItem };
        return new Meal(userId, "Déjeuner", MealType.Lunch, null, [item1, item2], DateTime.UtcNow, false);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ShouldReturnMealResponse_WhenRequestIsValid()
    {
        var userId = Guid.NewGuid();
        var foodItem = BuildFoodItem();
        var request = new CreateMealRequest("Déjeuner", MealType.Lunch, DateTime.UtcNow, null, false,
            [new MealItemRequest(foodItem.Id, 150f)]);

        _foodItemRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync([foodItem]);
        _mealRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Meal>())).Returns(Task.CompletedTask);

        var result = await _mealService.CreateAsync(userId, request);

        Assert.IsType<MealResponse>(result);
        _mealRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Meal>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenFoodItemNotFound()
    {
        var userId = Guid.NewGuid();
        var unknownId = Guid.NewGuid();
        var request = new CreateMealRequest("Déjeuner", MealType.Lunch, DateTime.UtcNow, null, false,
            [new MealItemRequest(unknownId, 150f)]);

        _foodItemRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync([]);

        await Assert.ThrowsAsync<NotFoundException>(() => _mealService.CreateAsync(userId, request));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenSavedLimitReached()
    {
        var user = BuildUser();
        var foodItem = BuildFoodItem();
        var request = new CreateMealRequest("Déjeuner", MealType.Lunch, DateTime.UtcNow, null, true,
            [new MealItemRequest(foodItem.Id, 150f)]);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mealRepositoryMock.Setup(r => r.CountSavedByUserIdAsync(user.Id)).ReturnsAsync(5);

        await Assert.ThrowsAsync<ForbiddenException>(() => _mealService.CreateAsync(user.Id, request));
    }

    // --- GetAllAsync ---

    [Fact]
    public async Task GetAllAsync_ShouldReturnMealList_WhenMealsExist()
    {
        var userId = Guid.NewGuid();
        var foodItem = BuildFoodItem();

        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, null, null))
            .ReturnsAsync([BuildMeal(userId, foodItem), BuildMeal(userId, foodItem)]);

        var result = await _mealService.GetAllAsync(userId, null, null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoMeals()
    {
        var userId = Guid.NewGuid();

        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, null, null)).ReturnsAsync([]);

        var result = await _mealService.GetAllAsync(userId, null, null);

        Assert.Empty(result);
    }

    // --- GetByIdAsync ---

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMealResponse_WhenMealBelongsToUser()
    {
        var userId = Guid.NewGuid();
        var meal = BuildMeal(userId, BuildFoodItem());

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);

        var result = await _mealService.GetByIdAsync(userId, meal.Id);

        Assert.IsType<MealResponse>(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenMealNotFound()
    {
        var mealId = Guid.NewGuid();

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(mealId)).ReturnsAsync((Meal?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _mealService.GetByIdAsync(Guid.NewGuid(), mealId));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenMealDoesNotBelongToUser()
    {
        var meal = BuildMeal(Guid.NewGuid(), BuildFoodItem());

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);

        await Assert.ThrowsAsync<ForbiddenException>(() => _mealService.GetByIdAsync(Guid.NewGuid(), meal.Id));
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ShouldReturnMealResponse_WhenMealBelongsToUser()
    {
        var userId = Guid.NewGuid();
        var meal = BuildMeal(userId, BuildFoodItem());
        var request = new UpdateMealRequest("Dîner", MealType.Dinner, null, null, null);

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);
        _mealRepositoryMock.Setup(r => r.UpdateAsync(meal)).Returns(Task.CompletedTask);

        var result = await _mealService.UpdateAsync(userId, meal.Id, request);

        Assert.IsType<MealResponse>(result);
        _mealRepositoryMock.Verify(r => r.UpdateAsync(meal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenMealNotFound()
    {
        var mealId = Guid.NewGuid();

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(mealId)).ReturnsAsync((Meal?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _mealService.UpdateAsync(Guid.NewGuid(), mealId, new UpdateMealRequest(null, null, null, null, null)));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenMealDoesNotBelongToUser()
    {
        var meal = BuildMeal(Guid.NewGuid(), BuildFoodItem());

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _mealService.UpdateAsync(Guid.NewGuid(), meal.Id, new UpdateMealRequest(null, null, null, null, null)));
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ShouldDelete_WhenMealBelongsToUser()
    {
        var userId = Guid.NewGuid();
        var meal = BuildMeal(userId, BuildFoodItem());

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);
        _mealRepositoryMock.Setup(r => r.DeleteAsync(meal.Id)).Returns(Task.CompletedTask);

        await _mealService.DeleteAsync(userId, meal.Id);

        _mealRepositoryMock.Verify(r => r.DeleteAsync(meal.Id), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenMealNotFound()
    {
        var mealId = Guid.NewGuid();

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(mealId)).ReturnsAsync((Meal?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _mealService.DeleteAsync(Guid.NewGuid(), mealId));
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenMealDoesNotBelongToUser()
    {
        var meal = BuildMeal(Guid.NewGuid(), BuildFoodItem());

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);

        await Assert.ThrowsAsync<ForbiddenException>(() => _mealService.DeleteAsync(Guid.NewGuid(), meal.Id));
    }

    // --- AddItemAsync ---

    [Fact]
    public async Task AddItemAsync_ShouldReturnMealResponse_WhenItemAdded()
    {
        var userId = Guid.NewGuid();
        var foodItem = BuildFoodItem();
        var meal = BuildMeal(userId, foodItem);
        var request = new AddMealItemRequest(foodItem.Id, 100f);

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(foodItem.Id)).ReturnsAsync(foodItem);
        _mealRepositoryMock.Setup(r => r.UpdateAsync(meal)).Returns(Task.CompletedTask);

        var result = await _mealService.AddItemAsync(userId, meal.Id, request);

        Assert.IsType<MealResponse>(result);
        _mealRepositoryMock.Verify(r => r.UpdateAsync(meal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddItemAsync_ShouldThrow_WhenMealNotFound()
    {
        var mealId = Guid.NewGuid();

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(mealId)).ReturnsAsync((Meal?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _mealService.AddItemAsync(Guid.NewGuid(), mealId, new AddMealItemRequest(Guid.NewGuid(), 100f)));
    }

    [Fact]
    public async Task AddItemAsync_ShouldThrow_WhenMealDoesNotBelongToUser()
    {
        var foodItem = BuildFoodItem();
        var meal = BuildMeal(Guid.NewGuid(), foodItem);

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _mealService.AddItemAsync(Guid.NewGuid(), meal.Id, new AddMealItemRequest(foodItem.Id, 100f)));
    }

    [Fact]
    public async Task AddItemAsync_ShouldThrow_WhenFoodItemNotFound()
    {
        var userId = Guid.NewGuid();
        var foodItem = BuildFoodItem();
        var meal = BuildMeal(userId, foodItem);
        var unknownFoodId = Guid.NewGuid();

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(unknownFoodId)).ReturnsAsync((FoodItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _mealService.AddItemAsync(userId, meal.Id, new AddMealItemRequest(unknownFoodId, 100f)));
    }

    // --- RemoveItemAsync ---

    [Fact]
    public async Task RemoveItemAsync_ShouldReturnMealResponse_WhenItemRemoved()
    {
        var userId = Guid.NewGuid();
        var foodItem = BuildFoodItem();
        var meal = BuildMealWithTwoItems(userId, foodItem);
        var itemId = meal.MealItems[0].Id;

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);
        _mealRepositoryMock.Setup(r => r.UpdateAsync(meal)).Returns(Task.CompletedTask);

        var result = await _mealService.RemoveItemAsync(userId, meal.Id, itemId);

        Assert.IsType<MealResponse>(result);
        _mealRepositoryMock.Verify(r => r.UpdateAsync(meal), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldThrow_WhenMealNotFound()
    {
        var mealId = Guid.NewGuid();

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(mealId)).ReturnsAsync((Meal?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _mealService.RemoveItemAsync(Guid.NewGuid(), mealId, Guid.NewGuid()));
    }

    [Fact]
    public async Task RemoveItemAsync_ShouldThrow_WhenMealDoesNotBelongToUser()
    {
        var foodItem = BuildFoodItem();
        var meal = BuildMeal(Guid.NewGuid(), foodItem);

        _mealRepositoryMock.Setup(r => r.GetByIdAsync(meal.Id)).ReturnsAsync(meal);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _mealService.RemoveItemAsync(Guid.NewGuid(), meal.Id, meal.MealItems[0].Id));
    }
}
