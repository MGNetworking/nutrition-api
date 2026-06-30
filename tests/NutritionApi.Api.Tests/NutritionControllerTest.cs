namespace NutritionApi.Api.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.Nutrition;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

public class NutritionControllerTest
{
    private readonly Mock<INutritionService> _mockNutritionService = new();
    private readonly NutritionController _controller;

    public NutritionControllerTest()
    {
        _controller = new NutritionController(_mockNutritionService.Object);
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

    // --- GetBilan ---

    [Fact]
    public async Task GetBilan_ShouldReturnOk_WhenDietExists()
    {
        var userId = SetControllerContext();
        var dietId = Guid.NewGuid();
        var period = "week";
        DateOnly? date = null;
        DateOnly? startDate = null;
        DateOnly? endDate = null;
        var expected = BuildBilanResponse();

        _mockNutritionService
            .Setup(s => s.GetBilanAsync(userId, dietId, period, date, startDate, endDate))
            .ReturnsAsync(expected);

        var result = await _controller.GetBilan(dietId, period, date, startDate, endDate);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
        _mockNutritionService.Verify(s => s.GetBilanAsync(userId, dietId, period, date, startDate, endDate), Times.Once);
    }
}
