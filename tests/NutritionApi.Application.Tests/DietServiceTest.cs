namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

public class DietServiceTest
{
    private readonly Mock<IDietRepository> _dietRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IDietPlanRepository> _dietPlanRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _userRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IWeightEntryRepository> _weightEntryRepositoryMock = new(MockBehavior.Strict);
    private readonly SubscriptionGuard _subscriptionGuard = new();
    private readonly DietService _dietService;

    public DietServiceTest()
    {
        _dietService = new DietService(
            _dietRepositoryMock.Object,
            _dietPlanRepositoryMock.Object,
            _userRepositoryMock.Object,
            _weightEntryRepositoryMock.Object,
            _subscriptionGuard);
    }

    private static (User user, DietPlan plan) BuildFixtures(string keycloakId = "keycloak-123")
    {
        var user = new User(
            keycloakId,
            new DateOnly(1990, 1, 1),
            Gender.Male,
            ActivityLevel.ModeratelyActive,
            180f,
            [],
            []);

        var macro = new MacroDistribution(30, 40, 30);

        var plan = new DietPlan(
            user.Id,
            "Test Plan",
            false,
            DietType.Balanced,
            Goal.WeightLoss,
            75f,
            macro);

        return (user, plan);
    }

    // --- LaunchAsync ---

    [Fact]
    public async Task LaunchAsync_ShouldReturnDietResponse_WhenPlanIsValid()
    {
        var (user, plan) = BuildFixtures();
        var weightEntry = new WeightEntry(user.Id, 80f, DateOnly.FromDateTime(DateTime.UtcNow));

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _dietRepositoryMock.Setup(r => r.GetActiveByUserIdAsync(user.Id)).ReturnsAsync((Diet?)null);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([weightEntry]);
        _dietRepositoryMock.Setup(r => r.AddAsync(It.IsAny<Diet>())).Returns(Task.CompletedTask);

        var result = await _dietService.LaunchAsync(user.Id, plan.Id);

        Assert.IsType<DietResponse>(result);
        _dietRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Diet>()), Times.Once);
    }

    [Fact]
    public async Task LaunchAsync_ShouldThrow_WhenPlanNotFound()
    {
        var (user, plan) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync((DietPlan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _dietService.LaunchAsync(user.Id, plan.Id));
    }

    [Fact]
    public async Task LaunchAsync_ShouldThrow_WhenPlanDoesNotBelongToUser()
    {
        var (_, plan) = BuildFixtures();
        var otherUserId = Guid.NewGuid();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        await Assert.ThrowsAsync<ForbiddenException>(() => _dietService.LaunchAsync(otherUserId, plan.Id));
    }

    [Fact]
    public async Task LaunchAsync_ShouldThrow_WhenActiveDietAlreadyExists()
    {
        var (user, plan) = BuildFixtures();
        var activeDiet = new Diet(user.Id, "Active", DietType.Balanced, Goal.Maintenance, 70f, 2000, new MacroDistribution(20, 50, 30));

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _dietRepositoryMock.Setup(r => r.GetActiveByUserIdAsync(user.Id)).ReturnsAsync(activeDiet);

        await Assert.ThrowsAsync<ConflictException>(() => _dietService.LaunchAsync(user.Id, plan.Id));
    }

    [Fact]
    public async Task LaunchAsync_ShouldThrow_WhenNoWeightEntry()
    {
        var (user, plan) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _dietRepositoryMock.Setup(r => r.GetActiveByUserIdAsync(user.Id)).ReturnsAsync((Diet?)null);
        _weightEntryRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        await Assert.ThrowsAsync<UnprocessableException>(() => _dietService.LaunchAsync(user.Id, plan.Id));
    }
}
