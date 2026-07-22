namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

public class DietPlansServiceTest
{
    private readonly Mock<IDietPlanRepository> _dietPlanRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserRepository> _userRepositoryMock = new(MockBehavior.Strict);
    private readonly SubscriptionGuard _subscriptionGuard = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly DietPlansService _dietPlansService;

    public DietPlansServiceTest()
    {
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

        _dietPlansService = new DietPlansService(
            _dietPlanRepositoryMock.Object,
            _userRepositoryMock.Object,
            _subscriptionGuard,
            _unitOfWorkMock.Object);
    }

    private static (User user, DietPlan plan, CreateDietPlanRequest createRequest, UpdateDietPlanRequest updateRequest) BuildFixtures(string keycloakId = "keycloak-123")
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

        var createRequest = new CreateDietPlanRequest(
            "Plan-name",
            DietType.Vegan,
            Goal.Maintenance,
            100f,
            MacroDistributionDto.From(macro));

        var updateRequest = new UpdateDietPlanRequest(
            "Plan-name",
            DietType.Vegan,
            Goal.Maintenance,
            100f,
            MacroDistributionDto.From(macro));

        return (user, plan, createRequest, updateRequest);
    }

    // --- CreateAsync ---

    [Fact]
    public async Task CreateAsync_ShouldReturnDietPlanResponse_WhenRequestIsValid()
    {
        var (user, _, createRequest, _) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.CountByUserIdAsync(user.Id)).ReturnsAsync(0);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _dietPlanRepositoryMock.Setup(r => r.AddAsync(It.IsAny<DietPlan>())).Returns(Task.CompletedTask);

        var result = await _dietPlansService.CreateAsync(user.Id, createRequest);

        Assert.IsType<DietPlanResponse>(result);
        _dietPlanRepositoryMock.Verify(r => r.CountByUserIdAsync(user.Id), Times.Once);
        _userRepositoryMock.Verify(r => r.GetByIdAsync(user.Id), Times.Once);
        _dietPlanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<DietPlan>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenUserNotFound()
    {
        var (user, _, createRequest, _) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.CountByUserIdAsync(user.Id)).ReturnsAsync(0);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _dietPlansService.CreateAsync(user.Id, createRequest));
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenMacrosDoNotSumTo100()
    {
        var (user, _, _, _) = BuildFixtures();
        var invalidMacros = new MacroDistributionDto(10, 20, 30); // somme = 60
        var request = new CreateDietPlanRequest("Plan", DietType.Balanced, Goal.Maintenance, null, invalidMacros);

        _dietPlanRepositoryMock.Setup(r => r.CountByUserIdAsync(user.Id)).ReturnsAsync(0);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

        await Assert.ThrowsAsync<ArgumentException>(() => _dietPlansService.CreateAsync(user.Id, request));
    }

    // --- GetUserPlansAsync ---

    [Fact]
    public async Task GetUserPlansAsync_ShouldReturnPlans_WhenUserHasPlans()
    {
        var (user, plan, _, _) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([plan]);

        var result = await _dietPlansService.GetUserPlansAsync(user.Id);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetUserPlansAsync_ShouldReturnEmptyList_WhenUserHasNoPlans()
    {
        var (user, _, _, _) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByUserIdAsync(user.Id)).ReturnsAsync([]);

        var result = await _dietPlansService.GetUserPlansAsync(user.Id);

        Assert.Empty(result);
    }

    // --- GetTemplatesAsync ---

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnTemplates_WhenTemplatesExist()
    {
        var (user, _, _, _) = BuildFixtures();
        user.ChangeSubscriptionTier(SubscriptionTier.Pro);

        var template = new DietPlan(null, "Template", true, DietType.Balanced, Goal.Maintenance, 70f, new MacroDistribution(20, 50, 30));

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _dietPlanRepositoryMock.Setup(r => r.GetTemplatesAsync()).ReturnsAsync([template]);

        var result = await _dietPlansService.GetTemplatesAsync(user.Id);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldReturnEmptyList_WhenNoTemplatesExist()
    {
        var (user, _, _, _) = BuildFixtures();
        user.ChangeSubscriptionTier(SubscriptionTier.Pro);

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _dietPlanRepositoryMock.Setup(r => r.GetTemplatesAsync()).ReturnsAsync([]);

        var result = await _dietPlansService.GetTemplatesAsync(user.Id);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTemplatesAsync_ShouldThrow_WhenUserNotFound()
    {
        var (user, _, _, _) = BuildFixtures();

        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _dietPlansService.GetTemplatesAsync(user.Id));
    }

    // --- UpdateAsync ---

    [Fact]
    public async Task UpdateAsync_ShouldReturnUpdatedPlan_WhenRequestIsValid()
    {
        var (user, plan, _, updateRequest) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _dietPlanRepositoryMock.Setup(r => r.UpdateAsync(plan)).Returns(Task.CompletedTask);

        var result = await _dietPlansService.UpdateAsync(user.Id, plan.Id, updateRequest);

        Assert.IsType<DietPlanResponse>(result);
        _dietPlanRepositoryMock.Verify(r => r.UpdateAsync(plan), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenPlanNotFound()
    {
        var (user, plan, _, updateRequest) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync((DietPlan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _dietPlansService.UpdateAsync(user.Id, plan.Id, updateRequest));
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenPlanDoesNotBelongToUser()
    {
        var (_, plan, _, updateRequest) = BuildFixtures();
        var otherUserId = Guid.NewGuid();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        await Assert.ThrowsAsync<ForbiddenException>(() => _dietPlansService.UpdateAsync(otherUserId, plan.Id, updateRequest));
    }

    // --- DeleteAsync ---

    [Fact]
    public async Task DeleteAsync_ShouldDelete_WhenPlanExists()
    {
        var (user, plan, _, _) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _dietPlanRepositoryMock.Setup(r => r.DeleteAsync(plan.Id)).Returns(Task.CompletedTask);

        await _dietPlansService.DeleteAsync(user.Id, plan.Id);

        _dietPlanRepositoryMock.Verify(r => r.DeleteAsync(plan.Id), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenPlanNotFound()
    {
        var (user, plan, _, _) = BuildFixtures();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync((DietPlan?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _dietPlansService.DeleteAsync(user.Id, plan.Id));
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenPlanDoesNotBelongToUser()
    {
        var (_, plan, _, _) = BuildFixtures();
        var otherUserId = Guid.NewGuid();

        _dietPlanRepositoryMock.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        await Assert.ThrowsAsync<ForbiddenException>(() => _dietPlansService.DeleteAsync(otherUserId, plan.Id));
    }
}
