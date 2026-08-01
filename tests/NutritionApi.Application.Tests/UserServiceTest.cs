namespace NutritionApi.Application.Tests;

using Moq;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

[Trait("Level", "1")]
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
        var DietaryPreferences = new List<DietaryPreference>();

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

    // --- AddWeightEntryAsync ---

    [Fact]
    public async Task AddWeightEntryAsync_Success_ReturnsWeightEntryResponse()
    {
        var (user, _, _) = UserHelper();
        var weightEntry = new WeightEntry(user.Id, 75f, DateOnly.FromDateTime(DateTime.UtcNow));
        var request = new AddWeightEntryRequest(weightEntry.Weight, weightEntry.MeasuredAt);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(user);

        _weightEntryRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<WeightEntry>()))
            .Returns(Task.CompletedTask);


        // Act 
        var result = await _userService.AddWeightEntryAsync(user.Id, request);

        // Assert
        var response = Assert.IsType<WeightEntryResponse>(result);
        Assert.Equal(weightEntry.Weight, response.Weight);
        Assert.Equal(weightEntry.MeasuredAt, response.MeasuredAt);
    }

    [Fact]
    public async Task AddWeightEntryAsync_DuplicateDate_ThrowsConflictException()
    {
        // Arrange
        var (user, _, _) = UserHelper();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        var existingEntry = new WeightEntry(user.Id, 75f, date);
        var request = new AddWeightEntryRequest(80f, date);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        _weightEntryRepositoryMock
            .Setup(r => r.GetByUserIdAndDateAsync(user.Id, date))
            .ReturnsAsync(existingEntry);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _userService.AddWeightEntryAsync(user.Id, request));

        _weightEntryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WeightEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AddWeightEntryAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AddWeightEntryRequest(75f);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.AddWeightEntryAsync(userId, request));

        _weightEntryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<WeightEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // --- GetWeightHistoryAsync ---

    [Fact]
    public async Task GetWeightHistoryAsync_Success_ReturnsWeightEntryList()
    {
        // Arrange
        var (user, _, _) = UserHelper();
        var entries = new List<WeightEntry>
        {
            new WeightEntry(user.Id, 75f, new DateOnly(2024, 1, 1)),
            new WeightEntry(user.Id, 76f, new DateOnly(2024, 1, 8)),
        };

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        _weightEntryRepositoryMock
            .Setup(r => r.GetByUserIdAsync(user.Id))
            .ReturnsAsync(entries);

        // Act
        var result = await _userService.GetWeightHistoryAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(75f, result[0].Weight);
        Assert.Equal(76f, result[1].Weight);

        _weightEntryRepositoryMock.Verify(r => r.GetByUserIdAsync(user.Id), Times.Once);
    }

    [Fact]
    public async Task GetWeightHistoryAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.GetWeightHistoryAsync(userId));

        _weightEntryRepositoryMock.Verify(r => r.GetByUserIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    // --- UpdateWeightEntryAsync ---

    [Fact]
    public async Task UpdateWeightEntryAsync_Success_ReturnsWeightEntryResponse()
    {
        // Arrange
        var (user, _, _) = UserHelper();
        var entry = new WeightEntry(user.Id, 75f, new DateOnly(2024, 1, 1));
        var request = new UpdateWeightEntryRequest(80f, new DateOnly(2024, 1, 8));

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        _weightEntryRepositoryMock
            .Setup(r => r.GetByIdAsync(entry.Id))
            .ReturnsAsync(entry);

        _weightEntryRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<WeightEntry>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _userService.UpdateWeightEntryAsync(user.Id, entry.Id, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.Weight, result.Weight);
        Assert.Equal(request.MeasuredAt, result.MeasuredAt);

        _weightEntryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WeightEntry>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateWeightEntryAsync_EntryNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var (user, _, _) = UserHelper();
        var entryId = Guid.NewGuid();
        var request = new UpdateWeightEntryRequest(80f, new DateOnly(2024, 1, 8));

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(user.Id))
            .ReturnsAsync(user);

        _weightEntryRepositoryMock
            .Setup(r => r.GetByIdAsync(entryId))
            .ReturnsAsync((WeightEntry?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateWeightEntryAsync(user.Id, entryId, request));

        _weightEntryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WeightEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateWeightEntryAsync_UserNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var request = new UpdateWeightEntryRequest(80f, new DateOnly(2024, 1, 8));

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _userService.UpdateWeightEntryAsync(userId, entryId, request));

        _weightEntryRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _weightEntryRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<WeightEntry>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

}
