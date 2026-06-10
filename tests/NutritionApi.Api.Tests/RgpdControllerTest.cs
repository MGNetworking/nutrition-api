namespace NutritionApi.Api.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

public class RgpdControllerTest
{
    private readonly RgpdController _rgpdController;
    private readonly Mock<IRgpdService> _rgpdService = new();

    public RgpdControllerTest()
    {
        _rgpdController = new RgpdController(_rgpdService.Object);
    }

    private string SetControllerContextClaim(string kcUserId = null!, Guid? userId = null)
    {
        var useridKc = kcUserId ?? "keycloak-user-123";
        var claims = new List<Claim> { new("sub", useridKc) };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };

        if (userId.HasValue)
            httpContext.Items["UserId"] = userId.Value;

        _rgpdController.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return useridKc;
    }


    // --- RgpdExportUserDataAsync ---

    [Fact]
    public async Task RgpdExportUserDataAsync_Success_ReturnsUserExportResponse()
    {
        // Arrange
        var userKcId = this.SetControllerContextClaim("keycloak-123");

        var user = new User(
                keycloakId: userKcId,
                birthDate: new DateOnly(1990, 1, 1),
                gender: Gender.Male,
                activityLevel: ActivityLevel.LightlyActive,
                height: 180f,
                allergies: new List<Allergen>(),
                dietaryPreferences: new List<string>()
            );

        var userExportResponse = new UserExportResponse(
            Profile: new UserProfileResponse(
                Id: user.Id,
                BirthDate: user.BirthDate,
                Gender: user.Gender,
                ActivityLevel: user.ActivityLevel,
                Height: user.Height,
                Allergies: user.Allergies,
                DietaryPreferences: user.DietaryPreferences,
                SubscriptionTier: user.SubscriptionTier,
                CreatedAt: user.CreatedAt),
            WeightHistory: [],
            DietPlans: [],
            Diets: [],
            Meals: [],
            SavedFoodItems: []);

        _rgpdService
            .Setup(s => s.ExportUserDataAsync(userKcId))
            .ReturnsAsync(userExportResponse);

        // Act
        var DataExport = await _rgpdController
            .GetUserRgpdExportData();

        // Assert
        Assert.IsType<UserExportResponse>(DataExport);
        Assert.Equal(userExportResponse, DataExport);

        _rgpdService.Verify(s => s.ExportUserDataAsync(userKcId), Times.Once());



    }
}
