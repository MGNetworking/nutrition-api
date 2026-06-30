namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

public class RgpdServiceTest
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IWeightEntryRepository> _weightEntryRepositoryMock = new();
    private readonly Mock<IDietPlanRepository> _dietPlanRepositoryMock = new();
    private readonly Mock<IDietRepository> _dietRepositoryMock = new();
    private readonly Mock<IMealRepository> _mealRepositoryMock = new();
    private readonly Mock<ISavedFoodItemRepository> _savedFoodItemRepositoryMock = new();
    private readonly Mock<IFoodItemRepository> _foodItemRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RgpdService _rgpdService;

    public RgpdServiceTest()
    {
        _rgpdService = new RgpdService(
            _userRepositoryMock.Object,
            _weightEntryRepositoryMock.Object,
            _dietPlanRepositoryMock.Object,
            _dietRepositoryMock.Object,
            _mealRepositoryMock.Object,
            _savedFoodItemRepositoryMock.Object,
            _foodItemRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    // --- DeleteUserAsync ---

    [Fact]
    public async Task DeleteUserAsync_ShouldMarkAsDeleted_WhenUserExists()
    {
        var keycloakId = "keycloak-123";
        var user = new User(keycloakId, new DateOnly(1990, 1, 1), Gender.Male, ActivityLevel.LightlyActive, 180f, [], []);

        _userRepositoryMock.Setup(r => r.GetByKeycloakIdAsync(keycloakId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _rgpdService.DeleteUserAsync(keycloakId);

        _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteUserAsync_ShouldThrowNotFoundException_WhenUserNotFound()
    {
        _userRepositoryMock.Setup(r => r.GetByKeycloakIdAsync("keycloak-123")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _rgpdService.DeleteUserAsync("keycloak-123"));
    }

    // --- ReactivateUserAsync ---

    [Fact]
    public async Task ReactivateUserAsync_ShouldReturnUserProfile_WhenUserExists()
    {
        var keycloakId = "keycloak-123";
        var user = new User(keycloakId, new DateOnly(1990, 1, 1), Gender.Male, ActivityLevel.LightlyActive, 180f, [], []);
        user.MarkAsDeleted();

        _userRepositoryMock.Setup(r => r.GetByKeycloakIdAsync(keycloakId)).ReturnsAsync(user);
        _userRepositoryMock.Setup(r => r.UpdateAsync(user)).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _rgpdService.ReactivateUserAsync(keycloakId);

        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        _userRepositoryMock.Verify(r => r.UpdateAsync(user), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task ReactivateUserAsync_ShouldThrowNotFoundException_WhenUserNotFound()
    {
        _userRepositoryMock.Setup(r => r.GetByKeycloakIdAsync("keycloak-123")).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _rgpdService.ReactivateUserAsync("keycloak-123"));
    }

    // --- ExportUserDataAsync ---

    [Fact]
    public async Task ExportUserDataAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _userRepositoryMock
            .Setup(r => r.GetByKeycloakIdAsync("keycloak-123"))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _rgpdService.ExportUserDataAsync("keycloak-123"));
    }

    [Fact]
    public async Task ExportUserDataAsync_Success_ReturnsAllUserData()
    {
        // Arrange
        var keycloakId = "keycloak-123";
        var user = new User(keycloakId, new DateOnly(1990, 1, 1), Gender.Male, ActivityLevel.LightlyActive, 180f, [], []);
        var macro = new MacroDistribution(40, 40, 20);
        var foodItemId = Guid.NewGuid();

        var weightEntry = new WeightEntry(user.Id, 75f, DateOnly.FromDateTime(DateTime.UtcNow));
        var dietPlan = new DietPlan(user.Id, "Plan test", false, DietType.Balanced, Goal.Maintenance, 75f, macro);
        var diet = new Diet(user.Id, "Diet test", DietType.Balanced, Goal.Maintenance, 75f, 2000, macro);
        var savedFoodItem = new SavedFoodItem(user.Id, foodItemId);
        var foodItem = new FoodItem("off-123", "Pâtes", 350f, 12, 70, 2, []);

        _userRepositoryMock.Setup(r => r.GetByKeycloakIdAsync(keycloakId)).ReturnsAsync(user);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([weightEntry]);
        _dietPlanRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([dietPlan]);
        _dietRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([diet]);
        _mealRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);
        _savedFoodItemRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([savedFoodItem]);
        _foodItemRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>())).ReturnsAsync([foodItem]);

        // Act
        var result = await _rgpdService.ExportUserDataAsync(keycloakId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Profile.Id);
        Assert.Single(result.WeightHistory);
        Assert.Single(result.DietPlans);
        Assert.Single(result.Diets);
        Assert.Empty(result.Meals);
        Assert.Single(result.SavedFoodItems);
        Assert.Equal(foodItem.Name, result.SavedFoodItems[0].Name);

        _foodItemRepositoryMock.Verify(r => r.GetByIdsAsync(
            It.Is<List<Guid>>(ids => ids.Contains(foodItemId))), Times.Once);
    }
}
