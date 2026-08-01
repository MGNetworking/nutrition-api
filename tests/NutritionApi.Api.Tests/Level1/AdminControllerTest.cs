namespace NutritionApi.Api.Tests.Level1;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

[Trait("Level", "1")]
public class AdminControllerTest
{
    private readonly Mock<IAdminService> _mockAdminService = new();
    private readonly AdminController _controller;

    public AdminControllerTest()
    {
        _controller = new AdminController(_mockAdminService.Object);
    }

    private void SetControllerContext()
    {
        var claims = new List<Claim>
        {
            new("sub", "keycloak-admin-123"),
            new(System.Security.Claims.ClaimTypes.Role, "admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static AdminDashboardResponse BuildDashboardResponse() => new(
        TotalUsers: 100,
        UsersByTier: new UsersByTierResponse(Free: 80, Pro: 15, Business: 5),
        NewUsersLast7Days: 10,
        ActiveDiets: 25,
        MealsLast7Days: 350,
        UsersInGracePeriod: 3
    );

    private static SystemHealthResponse BuildSystemHealthResponse() => new(
        FoodItemsCount: 50000,
        LastImportAt: DateTime.UtcNow.AddHours(-2),
        HangfireJobs: []
    );

    private static DietPlanResponse BuildDietPlanResponse() => new(
        Id: Guid.NewGuid(),
        Name: "Template Keto",
        DietType: DietType.Keto,
        Goal: Goal.WeightLoss,
        TargetWeight: null,
        MacroDistribution: new MacroDistributionDto(30, 10, 60),
        IsTemplate: true
    );

    // -------------------------------------------------------------------------
    // Dashboard & santé système
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetDashboard_WhenCalled_ReturnsOk()
    {
        // Arrange 
        var adminDash = BuildDashboardResponse();

        _mockAdminService
            .Setup(s => s.GetDashboardAsync())
            .ReturnsAsync(adminDash);

        // Act
        var result = await _controller.GetDashboard();

        // Assert
        var ok = Assert.IsType<OkObjectResult>( result );
        Assert.Equal(adminDash, ok.Value );

        _mockAdminService.Verify(s=>s.GetDashboardAsync(), Times.Once());

    }

    [Fact]
    public async Task GetSystemHealth_WhenCalled_ReturnsOk()
    {
        // Arrange
        var health = BuildSystemHealthResponse();

        _mockAdminService
            .Setup(s => s.GetSystemHealthAsync())
            .ReturnsAsync(health);

        // Act
        var result = await _controller.GetSystemHealth();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(health, ok.Value);

        _mockAdminService.Verify(s => s.GetSystemHealthAsync(), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Templates
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateTemplate_WhenValid_ReturnsCreated()
    {
        // Arrange
        var request = new CreateDietPlanRequest(
            Name: "Template Keto",
            DietType: DietType.Keto,
            Goal: Goal.WeightLoss,
            TargetWeight: null,
            MacroDistribution: new MacroDistributionDto(30, 10, 60)
        );
        var response = BuildDietPlanResponse();

        _mockAdminService
            .Setup(s => s.CreateTemplateAsync(request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.CreateTemplate(request);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(response, created.Value);

        _mockAdminService.Verify(s => s.CreateTemplateAsync(request), Times.Once);
    }

    [Fact]
    public async Task UpdateTemplate_WhenTemplateExists_ReturnsOk()
    {
        // Arrange
        var templateId = Guid.NewGuid();
        var request = new UpdateDietPlanRequest(
            Name: "Template Keto modifié",
            DietType: DietType.Keto,
            Goal: Goal.WeightLoss,
            TargetWeight: null,
            MacroDistribution: new MacroDistributionDto(30, 10, 60)
        );
        var response = BuildDietPlanResponse();

        _mockAdminService
            .Setup(s => s.UpdateTemplateAsync(templateId, request))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UpdateTemplate(templateId, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);

        _mockAdminService.Verify(s => s.UpdateTemplateAsync(templateId, request), Times.Once);
    }

    [Fact]
    public async Task DeleteTemplate_WhenTemplateExists_ReturnsNoContent()
    {
        // Arrange
        var templateId = Guid.NewGuid();

        _mockAdminService
            .Setup(s => s.DeleteTemplateAsync(templateId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteTemplate(templateId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        _mockAdminService.Verify(s => s.DeleteTemplateAsync(templateId), Times.Once);
    }
}
