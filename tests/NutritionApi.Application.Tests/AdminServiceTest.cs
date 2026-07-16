namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Enums;

public class AdminServiceTest
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IDietRepository> _dietRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IMealRepository> _mealRepositoryMock = new(MockBehavior.Strict);
    private readonly AdminService _adminService;

    public AdminServiceTest()
    {
        _adminService = new AdminService(
            _userRepositoryMock.Object,
            _dietRepositoryMock.Object,
            _mealRepositoryMock.Object);
    }

    private void SetupCounts(
        int free = 0, int pro = 0, int business = 0,
        int newUsers = 0, int gracePeriod = 0,
        int activeDiets = 0, int meals = 0)
    {
        _userRepositoryMock.Setup(r => r.CountByTierAsync(SubscriptionTier.Free)).ReturnsAsync(free);
        _userRepositoryMock.Setup(r => r.CountByTierAsync(SubscriptionTier.Pro)).ReturnsAsync(pro);
        _userRepositoryMock.Setup(r => r.CountByTierAsync(SubscriptionTier.Business)).ReturnsAsync(business);
        _userRepositoryMock.Setup(r => r.CountCreatedSinceAsync(It.IsAny<DateTime>())).ReturnsAsync(newUsers);
        _userRepositoryMock.Setup(r => r.CountInGracePeriodAsync()).ReturnsAsync(gracePeriod);
        _dietRepositoryMock.Setup(r => r.CountActiveAsync()).ReturnsAsync(activeDiets);
        _mealRepositoryMock.Setup(r => r.CountCreatedSinceAsync(It.IsAny<DateTime>())).ReturnsAsync(meals);
    }

    // --- GetDashboardAsync — chemin nominal ---

    [Fact]
    public async Task GetDashboardAsync_ShouldReturnAggregatedKpis_WhenDataExists()
    {
        SetupCounts(free: 100, pro: 30, business: 5, newUsers: 12, gracePeriod: 3, activeDiets: 42, meals: 250);

        var result = await _adminService.GetDashboardAsync();

        Assert.Equal(135, result.TotalUsers);
        Assert.Equal(100, result.UsersByTier.Free);
        Assert.Equal(30, result.UsersByTier.Pro);
        Assert.Equal(5, result.UsersByTier.Business);
        Assert.Equal(12, result.NewUsersLast7Days);
        Assert.Equal(42, result.ActiveDiets);
        Assert.Equal(250, result.MealsLast7Days);
        Assert.Equal(3, result.UsersInGracePeriod);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldQueryLast7Days_ForNewUsersAndMeals()
    {
        SetupCounts();
        var expectedSince = DateTime.UtcNow.AddDays(-7);

        await _adminService.GetDashboardAsync();

        _userRepositoryMock.Verify(r => r.CountCreatedSinceAsync(
            It.IsInRange(expectedSince.AddMinutes(-1), expectedSince.AddMinutes(1), Moq.Range.Inclusive)), Times.Once);
        _mealRepositoryMock.Verify(r => r.CountCreatedSinceAsync(
            It.IsInRange(expectedSince.AddMinutes(-1), expectedSince.AddMinutes(1), Moq.Range.Inclusive)), Times.Once);
    }

    // --- GetDashboardAsync — cas limites ---

    [Fact]
    public async Task GetDashboardAsync_ShouldReturnZeroedKpis_WhenNoData()
    {
        SetupCounts();

        var result = await _adminService.GetDashboardAsync();

        Assert.Equal(0, result.TotalUsers);
        Assert.Equal(0, result.UsersByTier.Free);
        Assert.Equal(0, result.NewUsersLast7Days);
        Assert.Equal(0, result.ActiveDiets);
        Assert.Equal(0, result.MealsLast7Days);
        Assert.Equal(0, result.UsersInGracePeriod);
    }

    [Fact]
    public async Task GetDashboardAsync_ShouldCallEachCountOnce()
    {
        SetupCounts();

        await _adminService.GetDashboardAsync();

        _userRepositoryMock.Verify(r => r.CountByTierAsync(SubscriptionTier.Free), Times.Once);
        _userRepositoryMock.Verify(r => r.CountByTierAsync(SubscriptionTier.Pro), Times.Once);
        _userRepositoryMock.Verify(r => r.CountByTierAsync(SubscriptionTier.Business), Times.Once);
        _userRepositoryMock.Verify(r => r.CountInGracePeriodAsync(), Times.Once);
        _dietRepositoryMock.Verify(r => r.CountActiveAsync(), Times.Once);
    }
}
