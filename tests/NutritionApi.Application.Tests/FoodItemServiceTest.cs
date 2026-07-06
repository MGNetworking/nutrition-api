namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

public class FoodItemServiceTest
{
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISavedFoodItemRepository> _savedFoodItemRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFoodCacheService> _foodCacheServiceMock = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _userRepositoryMock = new(MockBehavior.Strict);
    private readonly SubscriptionGuard _subscriptionGuard = new();
    private readonly FoodItemService _foodItemService;

    public FoodItemServiceTest()
    {
        _foodItemService = new FoodItemService(
            _foodItemRepositoryMock.Object,
            _savedFoodItemRepositoryMock.Object,
            _foodCacheServiceMock.Object,
            _userRepositoryMock.Object,
            _subscriptionGuard);
    }

    private static FoodItem BuildFoodItem()
        => new("off-001", "Poulet", 165f, 31, 0, 4, []);

    private static User BuildUser()
        => new("keycloak-123", new DateOnly(1990, 1, 1), Gender.Male, ActivityLevel.ModeratelyActive, 180f, [], []);

    private static SavedFoodItem BuildSavedFoodItem(Guid userId, Guid foodItemId)
        => new(userId, foodItemId);

    // --- SearchAsync ---

    [Fact]
    public async Task SearchAsync_ShouldReturnCachedResults_WhenCacheHit()
    {
        var cached = new List<FoodItemSearchResponse> { new(Guid.NewGuid(), "Poulet", 165f, 31, 0, 4, []) };
        _foodCacheServiceMock.Setup(c => c.GetAsync("poulet")).ReturnsAsync(cached);

        var result = await _foodItemService.SearchAsync("poulet");

        Assert.Equal(1, result.Count);
        _foodItemRepositoryMock.Verify(r => r.SearchByKeywordAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SearchAsync_ShouldQueryDbAndCacheResults_WhenCacheMiss()
    {
        var foodItem = BuildFoodItem();
        _foodCacheServiceMock.Setup(c => c.GetAsync("poulet")).ReturnsAsync((List<FoodItemSearchResponse>?)null);
        _foodItemRepositoryMock.Setup(r => r.SearchByKeywordAsync("poulet", 20)).ReturnsAsync([foodItem]);
        _foodCacheServiceMock.Setup(c => c.SetAsync("poulet", It.IsAny<List<FoodItemSearchResponse>>())).Returns(Task.CompletedTask);

        var result = await _foodItemService.SearchAsync("poulet");

        Assert.Equal(1, result.Count);
        _foodCacheServiceMock.Verify(c => c.SetAsync("poulet", It.IsAny<List<FoodItemSearchResponse>>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldReturnEmptyList_WhenNoResults()
    {
        _foodCacheServiceMock.Setup(c => c.GetAsync("xyz")).ReturnsAsync((List<FoodItemSearchResponse>?)null);
        _foodItemRepositoryMock.Setup(r => r.SearchByKeywordAsync("xyz", 20)).ReturnsAsync([]);
        _foodCacheServiceMock.Setup(c => c.SetAsync("xyz", It.IsAny<List<FoodItemSearchResponse>>())).Returns(Task.CompletedTask);

        var result = await _foodItemService.SearchAsync("xyz");

        Assert.Empty(result);
    }

    // --- GetSavedAsync ---

    [Fact]
    public async Task GetSavedAsync_ShouldReturnList_WhenItemsExist()
    {
        var userId = Guid.NewGuid();
        var foodItem = BuildFoodItem();
        var savedFoodItem = BuildSavedFoodItem(userId, foodItem.Id);

        _savedFoodItemRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync([savedFoodItem]);
        _foodItemRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync([foodItem]);

        var result = await _foodItemService.GetSavedAsync(userId);

        Assert.Equal(1, result.Count);
    }

    [Fact]
    public async Task GetSavedAsync_ShouldReturnEmptyList_WhenNoItems()
    {
        var userId = Guid.NewGuid();
        _savedFoodItemRepositoryMock.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync([]);
        _foodItemRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync([]);

        var result = await _foodItemService.GetSavedAsync(userId);

        Assert.Empty(result);
    }

    // --- SaveAsync ---

    [Fact]
    public async Task SaveAsync_ShouldReturnResponse_WhenFoodItemExistsAndUnderLimit()
    {
        var user = BuildUser();
        var foodItem = BuildFoodItem();
        var request = new SaveFoodItemRequest(foodItem.Id);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _savedFoodItemRepositoryMock.Setup(r => r.GetByUserIdAndFoodItemIdAsync(user.Id, foodItem.Id)).ReturnsAsync((SavedFoodItem?)null);
        _savedFoodItemRepositoryMock.Setup(r => r.CountByUserIdAsync(user.Id)).ReturnsAsync(0);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(foodItem.Id)).ReturnsAsync(foodItem);
        _savedFoodItemRepositoryMock.Setup(r => r.AddAsync(It.IsAny<SavedFoodItem>())).Returns(Task.CompletedTask);

        var result = await _foodItemService.SaveAsync(user.Id, request);

        Assert.IsType<SavedFoodItemResponse>(result);
        _savedFoodItemRepositoryMock.Verify(r => r.AddAsync(It.IsAny<SavedFoodItem>()), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenFoodItemNotFound()
    {
        var user = BuildUser();
        var request = new SaveFoodItemRequest(Guid.NewGuid());

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _savedFoodItemRepositoryMock.Setup(r => r.GetByUserIdAndFoodItemIdAsync(user.Id, request.FoodItemId)).ReturnsAsync((SavedFoodItem?)null);
        _savedFoodItemRepositoryMock.Setup(r => r.CountByUserIdAsync(user.Id)).ReturnsAsync(0);
        _foodItemRepositoryMock.Setup(r => r.GetByIdAsync(request.FoodItemId)).ReturnsAsync((FoodItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _foodItemService.SaveAsync(user.Id, request));
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenAlreadySaved()
    {
        var user = BuildUser();
        var foodItem = BuildFoodItem();
        var request = new SaveFoodItemRequest(foodItem.Id);
        var existing = BuildSavedFoodItem(user.Id, foodItem.Id);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _savedFoodItemRepositoryMock.Setup(r => r.GetByUserIdAndFoodItemIdAsync(user.Id, foodItem.Id)).ReturnsAsync(existing);

        await Assert.ThrowsAsync<ConflictException>(() => _foodItemService.SaveAsync(user.Id, request));
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenLimitReached()
    {
        var user = BuildUser();
        var foodItem = BuildFoodItem();
        var request = new SaveFoodItemRequest(foodItem.Id);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _savedFoodItemRepositoryMock.Setup(r => r.GetByUserIdAndFoodItemIdAsync(user.Id, foodItem.Id)).ReturnsAsync((SavedFoodItem?)null);
        _savedFoodItemRepositoryMock.Setup(r => r.CountByUserIdAsync(user.Id)).ReturnsAsync(10);

        await Assert.ThrowsAsync<ForbiddenException>(() => _foodItemService.SaveAsync(user.Id, request));
    }

    // --- RemoveSavedAsync ---

    [Fact]
    public async Task RemoveSavedAsync_ShouldDelete_WhenBelongsToUser()
    {
        var userId = Guid.NewGuid();
        var savedFoodItem = BuildSavedFoodItem(userId, BuildFoodItem().Id);

        _savedFoodItemRepositoryMock.Setup(r => r.GetByIdAsync(savedFoodItem.Id)).ReturnsAsync(savedFoodItem);
        _savedFoodItemRepositoryMock.Setup(r => r.DeleteAsync(savedFoodItem.Id)).Returns(Task.CompletedTask);

        await _foodItemService.RemoveSavedAsync(userId, savedFoodItem.Id);

        _savedFoodItemRepositoryMock.Verify(r => r.DeleteAsync(savedFoodItem.Id), Times.Once);
    }

    [Fact]
    public async Task RemoveSavedAsync_ShouldThrow_WhenNotFound()
    {
        var savedId = Guid.NewGuid();
        _savedFoodItemRepositoryMock.Setup(r => r.GetByIdAsync(savedId)).ReturnsAsync((SavedFoodItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _foodItemService.RemoveSavedAsync(Guid.NewGuid(), savedId));
    }

    [Fact]
    public async Task RemoveSavedAsync_ShouldThrow_WhenDoesNotBelongToUser()
    {
        var savedFoodItem = BuildSavedFoodItem(Guid.NewGuid(), BuildFoodItem().Id);
        _savedFoodItemRepositoryMock.Setup(r => r.GetByIdAsync(savedFoodItem.Id)).ReturnsAsync(savedFoodItem);

        await Assert.ThrowsAsync<ForbiddenException>(() => _foodItemService.RemoveSavedAsync(Guid.NewGuid(), savedFoodItem.Id));
    }
}
