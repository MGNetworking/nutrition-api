namespace NutritionApi.Api.Tests.Level1;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

[Trait("Level", "1")]
public class MealsControllerTest
{
    private readonly Mock<IMealService> _mockMealService = new();
    private readonly MealsController _controller;

    public MealsControllerTest()
    {
        _controller = new MealsController(_mockMealService.Object);
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

    private static MealResponse BuildMealResponse() => new(
        Id: Guid.NewGuid(),
        Name: "Déjeuner",
        MealType: MealType.Lunch,
        ConsumedAt: DateTime.UtcNow,
        Notes: null,
        IsSaved: false,
        Items: []
    );

    private static CreateMealRequest BuildCreateMealRequest() => new(
        Name: "Déjeuner",
        MealType: MealType.Lunch,
        ConsumedAt: DateTime.UtcNow,
        Notes: null,
        IsSaved: false,
        Items: [new MealItemRequest(Guid.NewGuid(), 150f)]
    );

    // -------------------------------------------------------------------------
    // Repas
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Create_WhenValid_ReturnsCreated()
    {
        // Arrange
        var userId = SetControllerContext();
        var request = BuildCreateMealRequest();
        var expected = BuildMealResponse();

        _mockMealService
            .Setup(s => s.CreateAsync(userId, request))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(expected, created.Value);

        _mockMealService.Verify(s => s.CreateAsync(userId, request), Times.Once);
    }

    [Fact]
    public async Task GetAll_WhenUserHasMeals_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        bool? saved = null;
        DateOnly? date = null;
        var expected = new List<MealResponse> { BuildMealResponse() };

        _mockMealService
            .Setup(s => s.GetAllAsync(userId, saved, date))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetAll(saved, date);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockMealService.Verify(s => s.GetAllAsync(userId, saved, date), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenMealExists_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var mealId = Guid.NewGuid();
        var expected = BuildMealResponse();

        _mockMealService
            .Setup(s => s.GetByIdAsync(userId, mealId))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.GetById(mealId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockMealService.Verify(s => s.GetByIdAsync(userId, mealId), Times.Once);
    }

    [Fact]
    public async Task Update_WhenMealExists_ReturnsOk()
    {
        // Arrange
        var userId = SetControllerContext();
        var mealId = Guid.NewGuid();
        var request = new UpdateMealRequest(
            Name: "Dîner",
            MealType: MealType.Dinner,
            ConsumedAt: DateTime.UtcNow,
            Notes: null,
            IsSaved: false
        );
        var expected = BuildMealResponse();

        _mockMealService
            .Setup(s => s.UpdateAsync(userId, mealId, request))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.Update(mealId, request);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);

        _mockMealService.Verify(s => s.UpdateAsync(userId, mealId, request), Times.Once);
    }

    [Fact]
    public async Task Delete_WhenMealExists_ReturnsNoContent()
    {
        // Arrange
        var userId = SetControllerContext();
        var mealId = Guid.NewGuid();

        _mockMealService
            .Setup(s => s.DeleteAsync(userId, mealId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(mealId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        _mockMealService.Verify(s => s.DeleteAsync(userId, mealId), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Items
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddItem_WhenValid_ReturnsCreated()
    {
        // Arrange
        var userId = SetControllerContext();
        var mealId = Guid.NewGuid();
        var request = new AddMealItemRequest(FoodItemId: Guid.NewGuid(), Quantity: 150f);
        var expected = BuildMealResponse();

        _mockMealService
            .Setup(s => s.AddItemAsync(userId, mealId, request))
            .ReturnsAsync(expected);

        // Act
        var result = await _controller.AddItem(mealId, request);

        // Assert
        var created = Assert.IsType<CreatedResult>(result);
        Assert.Equal(expected, created.Value);

        _mockMealService.Verify(s => s.AddItemAsync(userId, mealId, request), Times.Once);
    }

    [Fact]
    public async Task RemoveItem_WhenItemExists_ReturnsNoContent()
    {
        // Arrange
        var userId = SetControllerContext();
        var mealId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _mockMealService
            .Setup(s => s.RemoveItemAsync(userId, mealId, itemId))
            .ReturnsAsync(BuildMealResponse());

        // Act
        var result = await _controller.RemoveItem(mealId, itemId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        _mockMealService.Verify(s => s.RemoveItemAsync(userId, mealId, itemId), Times.Once);
    }
}
