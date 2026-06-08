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

    private (User, UpdateUserProfileRequest, CreateUserProfileRequest) UserHelper(string kcId = null!)
    {
        var keycloakId = kcId ?? "keycloak-123";
        var birthDate = new DateOnly(1990, 1, 1);
        var gender = Gender.Male;
        var activityLevel = ActivityLevel.LightlyActive;
        var height = 180f;
        var allergies = new List<Allergen>();
        var DietaryPreferences = new List<string>();

        var createUser = new CreateUserProfileRequest(
                birthDate: birthDate,
                gender: gender,
                activityLevel: activityLevel,
                height: height,
                allergies: new List<Allergen>(),
                dietaryPreferences: DietaryPreferences,
                weight: 75f
                );

        var updateUser = new UpdateUserProfileRequest(
               BirthDate: birthDate,
               Gender: gender,
               ActivityLevel: ActivityLevel.LightlyActive,
               Height: height,
               Allergies: allergies,
               DietaryPreferences: DietaryPreferences
           );

        var user = new User(
                keycloakId: keycloakId,
                birthDate: birthDate,
                gender: gender,
                activityLevel: activityLevel,
                height: height,
                allergies: allergies,
                dietaryPreferences: DietaryPreferences
            );

        return (user, updateUser, createUser);
    }

    // --- CreateUserProfileAsync ---

    [Fact]
    public async Task CreateUserProfileAsync_Success_ReturnsUserProfileResponse()
    {
        // Arrange
        var keycloakId = "keycloak-123";
        var (_, _, CreateUser) = UserHelper(keycloakId);

        _userRepositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync((User?)null);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userService.CreateUserProfileAsync(keycloakId, CreateUser);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(CreateUser.birthDate, result.BirthDate);
        Assert.Equal(CreateUser.gender, result.Gender);
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
        var (user, _, CreateUser) = UserHelper(keycloakId);

        _userRepositoryMock
            .Setup(r => r.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync(user);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() =>
            _userService.CreateUserProfileAsync(keycloakId, CreateUser));

        _userRepositoryMock.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }



    // --- UpdateUserProfileAsync ---

    [Fact]
    public async Task UpdateUserProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        string keycloakId = "keycloak-123";
        var (_, updateUser, _) = UserHelper(keycloakId);

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
        var (user, updateUser, _) = UserHelper(keycloakId);

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

    // --- GetUserProfileAsync ---

    [Fact]
    public async Task GetUserProfileAsync_UserFound_ReturnsUserProfileResponse()
    {
        // Arrange
        string keycloakId = "keycloak-123";
        var (user, _, _) = UserHelper(keycloakId);

        _userRepositoryMock
            .Setup(s => s.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync(user);

        // Act 
        var result = await _userService.GetUserProfileAsync(keycloakId);

        // Assert
        Assert.NotNull(result);
        _userRepositoryMock.Verify(s => s.GetByKeycloakIdAsync(keycloakId), Times.Once());

    }

    [Fact]
    public async Task GetUserProfileAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        string keycloakId = "keycloak-123";

        _userRepositoryMock
            .Setup(s => s.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync((User?)null);

        // Act / Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetUserProfileAsync(keycloakId));

    }

    // --- DeleteUserAsync ---

    [Fact]
    public async Task DeleteUserAsync_Success_CompletesWithoutError()
    {
        // Arrange
        string keycloakId = "keycloak-123";
        var (user, _, _) = UserHelper(keycloakId);

        _userRepositoryMock
            .Setup(s => s.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync(user);

        // Act
        await _userService.DeleteUserAsync(keycloakId);

        // Assert
        Assert.NotNull(user.DeletedAt);
    }

    [Fact]
    public async Task DeleteUserAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        string keycloakId = "keycloak-123";
        _userRepositoryMock
            .Setup(s => s.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync((User?)null);

        // Act / Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.DeleteUserAsync(keycloakId));

    }

    // --- ReactivateUserAsync ---

    [Fact]
    public async Task ReactivateUserAsync_Success_ReturnsUserProfileResponse()
    {
        // Arrange 
        string keycloakId = "keycloak-123";
        var (user, _, _) = UserHelper(keycloakId);
        user.MarkAsDeleted();
        _userRepositoryMock
            .Setup(s => s.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync(user);

        // Act
        await _userService.ReactivateUserAsync(keycloakId);

        // Assert
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public async Task ReactivateUserAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        string keycloakId = "keycloak-123";
        _userRepositoryMock
            .Setup(s => s.GetByKeycloakIdAsync(keycloakId))
            .ReturnsAsync((User?)null);

        // Act / Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.ReactivateUserAsync(keycloakId));

    }

    // --- AddWeightEntryAsync ---

    [Fact]
    public async Task AddWeightEntryAsync_Success_ReturnsWeightEntryResponse()
        => throw new NotImplementedException();

    [Fact]
    public async Task AddWeightEntryAsync_DuplicateDate_ThrowsConflictException()
        => throw new NotImplementedException();

    [Fact]
    public async Task AddWeightEntryAsync_UserNotFound_ThrowsNotFoundException()
        => throw new NotImplementedException();

    // --- GetWeightHistoryAsync ---

    [Fact]
    public async Task GetWeightHistoryAsync_Success_ReturnsWeightEntryList()
        => throw new NotImplementedException();

    [Fact]
    public async Task GetWeightHistoryAsync_UserNotFound_ThrowsNotFoundException()
        => throw new NotImplementedException();

    // --- UpdateWeightEntryAsync ---

    [Fact]
    public async Task UpdateWeightEntryAsync_Success_ReturnsWeightEntryResponse()
        => throw new NotImplementedException();

    [Fact]
    public async Task UpdateWeightEntryAsync_EntryNotFound_ThrowsNotFoundException()
        => throw new NotImplementedException();

    [Fact]
    public async Task UpdateWeightEntryAsync_UserNotFound_ThrowsNotFoundException()
        => throw new NotImplementedException();

}
