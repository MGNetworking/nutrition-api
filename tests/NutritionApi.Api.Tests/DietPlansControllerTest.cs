namespace NutritionApi.Api.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

public class DietPlansControllerTest
{
    private readonly Mock<IDietPlanService> _mockDietPlanService = new();
    private readonly DietPlansController _controller;

    public DietPlansControllerTest()
    {
        _controller = new DietPlansController(_mockDietPlanService.Object);
    }

    /// <summary>
    /// Helper de simulation du middleware authentification 
    /// plus mapping de configuration du middleware UserResolutionMiddleware
    /// </summary>
    /// <returns></returns>
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

    private static DietPlanResponse BuildDietPlanResponse() => new(
        Id: Guid.NewGuid(),
        Name: "Mon plan",
        DietType: DietType.Balanced,
        Goal: Goal.WeightLoss,
        TargetWeight: 75f,
        MacroDistribution: new MacroDistributionDto(40f, 30f, 30f),
        IsTemplate: false
    );

    private static DietResponse BuildDietResponse() => new(
        Id: Guid.NewGuid(),
        Name: "Mon plan",
        DietType: DietType.Balanced,
        Goal: Goal.WeightLoss,
        TargetWeight: 75f,
        CalorieTarget: 2000f,
        MacroDistribution: new MacroDistributionDto(40f, 30f, 30f),
        Status: DietStatus.Active,
        StartDate: DateOnly.FromDateTime(DateTime.UtcNow),
        EndDate: null
    );

    // -------------------------------------------------------------------------
    // Plans personnels
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetAll_WhenUserHasPlans_ReturnsOk()
    {
        var userId = SetControllerContext();
        var expected = new List<DietPlanResponse> { BuildDietPlanResponse() };

        _mockDietPlanService
            .Setup(s => s.GetUserPlansAsync(userId))
            .ReturnsAsync(expected);

        var result = await _controller.GetAll();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        // Arrange
        var userId = SetControllerContext();
        var dieteCreate = new CreateDietPlanRequest(
            Name: "Mon plan",
            DietType: DietType.Balanced,
            Goal: Goal.WeightLoss,
            TargetWeight: 75f,
            MacroDistribution: new MacroDistributionDto(40f, 30f, 30f)
        );

        var dietPlan = new DietPlanResponse(
            Id: Guid.NewGuid(),
            Name: dieteCreate.Name,
            DietType: dieteCreate.DietType,
            Goal: dieteCreate.Goal,
            TargetWeight: dieteCreate.TargetWeight,
            MacroDistribution: dieteCreate.MacroDistribution,
            IsTemplate: false
        );

        _mockDietPlanService
            .Setup(s => s.CreateAsync(userId, dieteCreate))
            .ReturnsAsync(dietPlan);

        // Act
        var result = await _controller.Create(dieteCreate);

        // Assert
        var ok = Assert.IsType<CreatedResult>(result);
        Assert.Equal(dietPlan, ok.Value);
        _mockDietPlanService.Verify(s => s.CreateAsync(userId, dieteCreate), Times.Once);



    }

    [Fact]
    public async Task Update_WhenPlanExists_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var planId = Guid.NewGuid();
        var plan = new UpdateDietPlanRequest(
            Name: "Mon plan modifié",
            DietType: DietType.LowCarb,
            Goal: Goal.Maintenance,
            TargetWeight: 80f,
            MacroDistribution: new MacroDistributionDto(30f, 40f, 30f)
        );

        var planResponse = new DietPlanResponse(
            Id: planId,
            Name: plan.Name,
            DietType: plan.DietType,
            Goal: plan.Goal,
            TargetWeight: plan.TargetWeight,
            MacroDistribution: plan.MacroDistribution,
            IsTemplate: false
        );

        _mockDietPlanService
            .Setup(s => s.UpdateAsync(userId, planId, plan))
            .ReturnsAsync(planResponse);

        // Act
        var result = await _controller.Update(planId, plan);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(planResponse, ok.Value);
        _mockDietPlanService.Verify(s => s.UpdateAsync(userId, planId, plan));

    }

    [Fact]
    public async Task Delete_WhenPlanExists_ReturnsNoContent()
    {
        // Arrange
        var userId = SetControllerContext();
        var idDiete = Guid.NewGuid();

        _mockDietPlanService
            .Setup(s => s.DeleteAsync(userId, idDiete))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(idDiete);

        // Assert
        _mockDietPlanService.Verify(s => s.DeleteAsync(userId, idDiete), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Lancement
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Launch_WhenPlanValid_ReturnsCreated()
    {
        // Arrange
        var userId = SetControllerContext();
        var idPlan = Guid.NewGuid();
        var dietReponse = new DietResponse(
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

        _mockDietPlanService
            .Setup(s => s.LaunchAsync(userId, idPlan))
            .ReturnsAsync(dietReponse);

        // Act
        var result = await _controller.Launch(idPlan);

        // Assert
        var ok = Assert.IsType<CreatedResult>(result);
        Assert.Equal(dietReponse, ok.Value);

        _mockDietPlanService.Verify(s => s.LaunchAsync(userId, idPlan), Times.Once);

    }

    // -------------------------------------------------------------------------
    // Templates
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetTemplates_WhenUserIsPro_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var expected = new List<DietPlanResponse> { BuildDietPlanResponse() };

        _mockDietPlanService
            .Setup(s => s.GetTemplatesAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetTemplates();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockDietPlanService.Verify(s => s.GetTemplatesAsync(userId), Times.Once);

    }
}
