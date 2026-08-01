namespace NutritionApi.Api.Tests.Level1;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

[Trait("Level", "1")]
public class FoodItemsControllerTest
{
    private readonly Mock<IFoodItemService> _mockFoodItemService = new();
    private readonly FoodItemsController _controller;

    public FoodItemsControllerTest()
    {
        _controller = new FoodItemsController(_mockFoodItemService.Object);
    }

    private void SetControllerContext()
    {
        var claims = new List<Claim> { new("sub", "keycloak-user-123") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };
        httpContext.Items["UserId"] = Guid.NewGuid();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
    }

    private static FoodItemSearchResponse BuildFoodItemSearchResponse() => new(
        Id: Guid.NewGuid(),
        Name: "Poulet rôti",
        CaloriesPer100g: 165f,
        ProteinsPer100g: 31f,
        CarbsPer100g: 0f,
        FatsPer100g: 3.6f,
        AllergensTags: []
    );

    // -------------------------------------------------------------------------
    // Recherche
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Search_WhenKeywordMatches_ReturnsOk()
    {
        // Arrange
        SetControllerContext();
        var keyword = "poulet";
        var limit = 20;
        var expected = new List<FoodItemSearchResponse> { BuildFoodItemSearchResponse() };

        _mockFoodItemService
            .Setup(s => s.SearchAsync(keyword, limit))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Search(keyword, limit);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockFoodItemService.Verify(s => s.SearchAsync(keyword, limit), Times.Once);
    }

}
