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

[Trait("Level", "1")]
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


    private static UserProfileResponse BuildUserProfileResponse() => new(
        Id: Guid.NewGuid(),
        BirthDate: new DateOnly(1990, 1, 1),
        Gender: Gender.Male,
        ActivityLevel: ActivityLevel.LightlyActive,
        Height: 180f,
        Allergies: [],
        DietaryPreferences: [],
        SubscriptionTier: SubscriptionTier.Free,
        CreatedAt: DateTime.UtcNow);

    // --- DeleteUserAsync ---

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenUserExists()
    {
        var userKcId = SetControllerContextClaim("keycloak-123");

        _rgpdService
            .Setup(s => s.DeleteUserAsync(userKcId))
            .Returns(Task.CompletedTask);

        var result = await _rgpdController.Delete();

        Assert.IsType<NoContentResult>(result);
        _rgpdService.Verify(s => s.DeleteUserAsync(userKcId), Times.Once);
    }

    // --- ReactivateUserAsync ---

    [Fact]
    public async Task Reactivate_ShouldReturnOk_WhenUserIsInGracePeriod()
    {
        var userKcId = SetControllerContextClaim("keycloak-123");
        var expected = BuildUserProfileResponse();

        _rgpdService
            .Setup(s => s.ReactivateUserAsync(userKcId))
            .ReturnsAsync(expected);

        var result = await _rgpdController.Reactivate();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
        _rgpdService.Verify(s => s.ReactivateUserAsync(userKcId), Times.Once);
    }

    // --- GetUserRgpdExportData ---

    [Fact]
    public async Task GetUserRgpdExportData_ShouldReturnZipFile_WhenUserExists()
    {
        var userKcId = SetControllerContextClaim("keycloak-123");

        var exportResponse = new UserExportResponse(
            Profile: BuildUserProfileResponse(),
            WeightHistory: [],
            DietPlans: [],
            Diets: [],
            Meals: [],
            SavedFoodItems: []);

        _rgpdService
            .Setup(s => s.ExportUserDataAsync(userKcId))
            .ReturnsAsync(exportResponse);

        var result = await _rgpdController.GetUserRgpdExportData();

        var file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("application/zip", file.ContentType);
        Assert.StartsWith("export-", file.FileDownloadName);
        Assert.EndsWith(".zip", file.FileDownloadName);
        _rgpdService.Verify(s => s.ExportUserDataAsync(userKcId), Times.Once);
    }
}
