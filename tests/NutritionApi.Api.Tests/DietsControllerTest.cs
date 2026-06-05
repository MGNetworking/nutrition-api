namespace NutritionApi.Api.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.DTOS.Nutrition;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

public class DietsControllerTest
{
    private readonly Mock<IDietService> _mockDietService = new();
    private readonly DietsController _controller;

    public DietsControllerTest()
    {
        _controller = new DietsController(_mockDietService.Object);
    }

    private Guid SetControllerContext()
    {
        var userId = Guid.NewGuid();
        var claims = new List<Claim> { new("sub", "keycloak-user-123") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.Items["UserId"] = userId;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return userId;
    }

    private static DietResponse BuildDietResponse() => new(
        Id: Guid.NewGuid(),
        Name: "Mon régime",
        DietType: DietType.Balanced,
        Goal: Goal.WeightLoss,
        TargetWeight: 75f,
        CalorieTarget: 2000f,
        MacroDistribution: new MacroDistributionDto(40f, 30f, 30f),
        Status: DietStatus.Active,
        StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
        EndDate: null
    );

    private static NutritionBilanResponse BuildBilanResponse() => new(
        DietId: Guid.NewGuid(),
        StartDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7)),
        EndDate: DateOnly.FromDateTime(DateTime.UtcNow),
        TotalCalories: 14000f,
        TotalProteins: 700f,
        TotalCarbs: 1050f,
        TotalFats: 420f,
        DailyBreakdown: [],
        WeightProgression: []
    );

    // -------------------------------------------------------------------------
    // Régimes
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetActive_WhenDietExists_ReturnsOk()
    {

        var userId = SetControllerContext();
        var dietResponse = BuildDietResponse();
        _mockDietService
            .Setup(s => s.GetActiveAsync(userId))
            .ReturnsAsync(dietResponse);

        var reslut = await _controller.GetActive();

        var ok = Assert.IsType<OkObjectResult>(reslut);
        Assert.Equal(dietResponse, ok.Value);

        _mockDietService.Verify(s => s.GetActiveAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetHistory_WhenUserHasDiets_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var expected = new List<DietResponse> { BuildDietResponse() };

        _mockDietService
            .Setup(s => s.GetHistoryAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetHistory();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockDietService.Verify(s => s.GetHistoryAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenDietExists_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var dietId = Guid.NewGuid();
        var expected = BuildDietResponse();

        _mockDietService
            .Setup(s => s.GetByIdAsync(userId, dietId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetById(dietId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockDietService.Verify(s => s.GetByIdAsync(userId, dietId), Times.Once);
    }

    [Fact]
    public async Task Archive_WhenDietIsActive_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var dietId = Guid.NewGuid();
        var expected = BuildDietResponse();

        _mockDietService
            .Setup(s => s.ArchiveAsync(userId, dietId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Archive(dietId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockDietService.Verify(s => s.ArchiveAsync(userId, dietId), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Bilan
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetBilan_WhenDietExists_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var dietId = Guid.NewGuid();
        var period = "week";
        DateOnly? date = null;
        DateOnly? startDate = null;
        DateOnly? endDate = null;
        var expected = BuildBilanResponse();

        _mockDietService
            .Setup(s => s.GetBilanAsync(userId, dietId, period, date, startDate, endDate))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetBilan(dietId, period, date, startDate, endDate);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockDietService.Verify(s => s.GetBilanAsync(userId, dietId, period, date, startDate, endDate), Times.Once);
    }
}
