namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

public class UserServiceTest
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IWeightEntryRepository> _weightEntryRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UserService _userService;

    public UserServiceTest()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _weightEntryRepositoryMock = new Mock<IWeightEntryRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _userService = new UserService(
                  _userRepositoryMock.Object,
                  _weightEntryRepositoryMock.Object,
                  _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task CreateUserProfileAsync_Success_ReturnsUserProfileResponse()
    {
        // Arrange
        var keycloakId = "keycloak-123";
        var request = new CreateUserProfileRequest(
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.LightlyActive,
            height: 180f,
            allergies: new List<Allergen>(),
            dietaryPreferences: new List<string>(),
            weight: 75f
        );

        _userRepositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync((User?)null);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userService.CreateUserProfileAsync(keycloakId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.birthDate, result.BirthDate);
        Assert.Equal(request.gender, result.Gender);
        Assert.Equal(SubscriptionTier.Free, result.SubscriptionTier);

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _weightEntryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WeightEntry>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateUserProfileAsync_UserAlreadyExists_ThrowsConflictException()
    {
        // Arrange
        var keycloakId = "keycloak-123";
        var request = new CreateUserProfileRequest(
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.LightlyActive,
            height: 180f,
            allergies: new List<Allergen>(),
            dietaryPreferences: new List<string>(),
            weight: 75f
        );

        var existingUser = new User(keycloakId, new DateOnly(1990, 1, 1), Gender.Male,
            ActivityLevel.LightlyActive, 180f, new List<Allergen>(), new List<string>());

        _userRepositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync(existingUser);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _userService.CreateUserProfileAsync(keycloakId, request));

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }



    [Fact]
    public async Task UpdateUserProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        string keycloakId = "keycloak-123";
        var updateUser = new UpdateUserProfileRequest(
            BirthDate: new DateOnly(1990, 1, 1),
            Gender: Gender.Male,
            ActivityLevel: ActivityLevel.LightlyActive,
            Height: 180f,
            Allergies: new List<Allergen>(),
            DietaryPreferences: new List<string>()
        );

        _userRepositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync((User)null!);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateUserProfileAsync(keycloakId, updateUser));

    }

    [Fact]
    public async Task UpdateUserProfileAsync_Succes_ReturnUserProfile()
    {

        // Arrange
        string keycloakId = "keycloak-123";
        var updateUser = new UpdateUserProfileRequest(
            BirthDate: new DateOnly(1990, 1, 1),
            Gender: Gender.Male,
            ActivityLevel: ActivityLevel.LightlyActive,
            Height: 180f,
            Allergies: new List<Allergen>(),
            DietaryPreferences: new List<string>()
        );

        var user = new User(
            keycloakId: keycloakId,
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.LightlyActive,
            height: 180f,
            allergies: new List<Allergen>(),
            dietaryPreferences: new List<string>()
        );

        _userRepositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync(user);

        _userRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<User>()))
            .Returns(Task.FromResult(user));

        _unitOfWorkMock.Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userService.UpdateUserProfileAsync(keycloakId, updateUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(updateUser.BirthDate, result.BirthDate);
        Assert.Equal(updateUser.Gender, result.Gender);
        Assert.Equal(updateUser.ActivityLevel, result.ActivityLevel);
        Assert.Equal(updateUser.Height, result.Height);
        Assert.Equal(updateUser.Allergies, result.Allergies);
        Assert.Equal(updateUser.DietaryPreferences, result.DietaryPreferences);

        _userRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<User>()), Times.Once());
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(), Times.Once());
    }

}
