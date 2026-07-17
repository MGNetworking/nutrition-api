namespace NutritionApi.Api.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NutritionApi.Api.Controllers;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Enums;
using System.Security.Claims;

public class UsersControllerTest
{
    private readonly Mock<IUserService> _mockUserService = new();
    private readonly Mock<IFoodItemService> _mockFoodItemService = new();
    private readonly UsersController _usersController;

    public UsersControllerTest()
    {
        _usersController = new UsersController(
            _mockUserService.Object,
            _mockFoodItemService.Object);
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

        _usersController.ControllerContext = new ControllerContext { HttpContext = httpContext };
        return useridKc;
    }

    private (CreateUserProfileRequest Request, UserProfileResponse Response, UpdateUserProfileRequest UpdateRequest) BuildUserProfileData()
    {
        var userProfileRequest = new CreateUserProfileRequest(
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            height: 180,
            allergies: new List<Allergen>() { Allergen.Gluten },
            dietaryPreferences: new List<string>() { "Vegetarian" },
            weight: 75
        );

        var userProfileResponse = new UserProfileResponse(
            Id: Guid.NewGuid(),
            BirthDate: userProfileRequest.birthDate,
            Gender: userProfileRequest.gender,
            ActivityLevel: userProfileRequest.activityLevel,
            Height: userProfileRequest.height,
            Allergies: userProfileRequest.allergies,
            DietaryPreferences: userProfileRequest.dietaryPreferences,
            SubscriptionTier: SubscriptionTier.Free,
            CreatedAt: DateTime.UtcNow);

        var updateUserProfileRequest = new UpdateUserProfileRequest(
            BirthDate: userProfileRequest.birthDate,
            Gender: userProfileRequest.gender,
            ActivityLevel: userProfileRequest.activityLevel,
            Height: userProfileRequest.height,
            Allergies: userProfileRequest.allergies,
            DietaryPreferences: userProfileRequest.dietaryPreferences
        );

        return (userProfileRequest, userProfileResponse, updateUserProfileRequest);
    }

    // -------------------------------------------------------------------------
    // Profil
    // -------------------------------------------------------------------------

    [Fact]
    public async Task CreateUser_WhenProfileValid_ReturnsCreated()
    {
        // Arrange
        var userKcId = this.SetControllerContextClaim();
        var (userProfileRequest, userProfileResponse, _) = this.BuildUserProfileData();

        _mockUserService
            .Setup(s => s.CreateUserProfileAsync(userKcId, userProfileRequest))
            .ReturnsAsync(userProfileResponse);

        // Act
        var result = await _usersController.CreateUser(userProfileRequest);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(userProfileResponse, created.Value);
        _mockUserService.Verify(s => s.CreateUserProfileAsync(userKcId, userProfileRequest), Times.Once);
    }

    [Fact]
    public async Task GetProfile_WhenUserExists_ReturnsOk()
    {
        // Arrange
        var userKcId = this.SetControllerContextClaim();
        var (_, userProfileResponse, _) = this.BuildUserProfileData();

        _mockUserService
            .Setup(s => s.GetUserProfileAsync(userKcId))
            .ReturnsAsync(userProfileResponse);

        // Act
        var result = await _usersController.GetProfile();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(userProfileResponse, ok.Value);
        _mockUserService.Verify(s => s.GetUserProfileAsync(userKcId), Times.Once);
    }

    [Fact]
    public async Task UpdateProfile_WhenUserExists_ReturnsOk()
    {
        // Arrange
        var userKcId = this.SetControllerContextClaim();
        var (_, userProfileResponse, updateUserProfileRequest) = this.BuildUserProfileData();

        _mockUserService
            .Setup(s => s.UpdateUserProfileAsync(userKcId, updateUserProfileRequest))
            .ReturnsAsync(userProfileResponse);

        // Act
        var result = await _usersController.PutProfile(updateUserProfileRequest);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(userProfileResponse, ok.Value);
        _mockUserService.Verify(s => s.UpdateUserProfileAsync(userKcId, updateUserProfileRequest), Times.Once);
    }

    // -------------------------------------------------------------------------
    // Pesées
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddWeightEntry_WhenValid_ReturnsCreated()
    {
        // Arrange
        this.SetControllerContextClaim("KcUserId", Guid.NewGuid());
        var userProfile = UserContextExtensions.GetUserId(_usersController.HttpContext);
        var date = DateOnly.FromDateTime(DateTime.Now);
        var weightRequest = new AddWeightEntryRequest(100, date);
        var weightResponse = new WeightEntryResponse(Guid.NewGuid(), weightRequest.Weight, date);

        _mockUserService
            .Setup(s => s.AddWeightEntryAsync(userProfile, weightRequest))
            .ReturnsAsync(weightResponse);

        // Act
        var result = await _usersController.AddWeightEntry(weightRequest);

        // Assert 
        var ok = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(weightResponse, ok.Value);
        _mockUserService.Verify(s => s.AddWeightEntryAsync(userProfile, weightRequest), Times.Once);
    }

    [Fact]
    public async Task GetWeightHistory_WhenUserHasEntries_ReturnsOk()
    {
        // Arrange
        this.SetControllerContextClaim("KcUserId", Guid.NewGuid());
        var userId = UserContextExtensions.GetUserId(_usersController.HttpContext);
        var date = DateOnly.FromDateTime(DateTime.Now);

        var listWeight = new List<WeightEntryResponse> { new WeightEntryResponse(Guid.NewGuid(), 100, date) };

        _mockUserService
            .Setup(s => s.GetWeightHistoryAsync(userId))
            .ReturnsAsync(listWeight);

        // Act
        var result = await _usersController.GetWeightHistory();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(listWeight, ok.Value);
        _mockUserService.Verify(s => s.GetWeightHistoryAsync(userId), Times.Once);

    }

    [Fact]
    public async Task UpdateWeightEntry_WhenEntryExists_ReturnsOk()
    {
        // Arrange
        this.SetControllerContextClaim("KcUserId", Guid.NewGuid());
        var userId = UserContextExtensions.GetUserId(_usersController.HttpContext);
        var WeightId = Guid.NewGuid();
        var updateWeight = new UpdateWeightEntryRequest(100, DateOnly.FromDateTime(DateTime.Now));
        var responsWeight = new WeightEntryResponse(WeightId, 100, updateWeight.MeasuredAt);
        var entryId = Guid.NewGuid();

        _mockUserService
            .Setup(s => s.UpdateWeightEntryAsync(userId, entryId, updateWeight))
            .ReturnsAsync(responsWeight);

        // Act 
        var result = await _usersController.UpdateWeightEntry(entryId, updateWeight);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(responsWeight, ok.Value);
        _mockUserService.Verify(s => s.UpdateWeightEntryAsync(userId, entryId, updateWeight), Times.Once);

    }

    // -------------------------------------------------------------------------
    // Aliments favoris
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetSavedFoodItems_WhenUserExists_ReturnsOk()
    {
        // Arrange
        this.SetControllerContextClaim("KcUserId", Guid.NewGuid());
        var userId = UserContextExtensions.GetUserId(_usersController.HttpContext);

        var expected = new List<SavedFoodItemResponse>();
        _mockFoodItemService
            .Setup(s => s.GetSavedAsync(userId))
            .ReturnsAsync(expected);

        // Act
        var result = await _usersController.GetSavedFoodItems();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
        _mockFoodItemService.Verify(s => s.GetSavedAsync(userId), Times.Once);
    }

    [Fact]
    public async Task SaveFoodItem_WhenValid_ReturnsCreated()
    {
        // Arrange
        this.SetControllerContextClaim("KcUserId", Guid.NewGuid());
        var userId = UserContextExtensions.GetUserId(_usersController.HttpContext);
        var idItem = Guid.NewGuid();
        var itemRequest = new SaveFoodItemRequest(idItem);
        var itemResponse = new SavedFoodItemResponse(Guid.NewGuid(), idItem, "Test Food", 250, DateTime.UtcNow);


        _mockFoodItemService
            .Setup(s => s.SaveAsync(userId, itemRequest))
            .ReturnsAsync(itemResponse);

        // Act
        var result = await _usersController.SaveFoodItem(itemRequest);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(itemResponse, created.Value);

        _mockFoodItemService.Verify(s => s.SaveAsync(userId, itemRequest), Times.Once);
    }

    [Fact]
    public async Task RemoveSavedFoodItem_WhenExists_ReturnsNoContent()
    {
        // Arrange
        this.SetControllerContextClaim("KcUserId", Guid.NewGuid());
        var userId = UserContextExtensions.GetUserId(_usersController.HttpContext);
        var idItem = Guid.NewGuid();

        _mockFoodItemService
            .Setup(s => s.RemoveSavedAsync(userId, idItem))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _usersController.RemoveSavedFoodItem(idItem);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockFoodItemService.Verify(s => s.RemoveSavedAsync(userId, idItem), Times.Once);
    }
}