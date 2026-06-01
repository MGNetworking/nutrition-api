namespace NutritionApi.Application.Services;

using Interfaces;
using Interfaces.Repositories;
using Interfaces.Services;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Exceptions;
using NutritionApi.Domain.Entity;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository user,
        IWeightEntryRepository weight,
        IUnitOfWork unitOfWork)
    {
        _userRepository = user;
        _weightEntryRepository = weight;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserProfileResponse> CreateUserProfileAsync(
        string keycloakId,
        CreateUserProfileRequest request)
    {
        var existing = await _userRepository.GetByKeycloakIdAsync(keycloakId);

        if (existing is not null)
            throw new ConflictException("User profile already exists.");

        var user = new User(
            keycloakId,
            request.birthDate,
            request.gender,
            request.activityLevel,
            request.height,
            request.allergies,
            request.dietaryPreferences);

        var weightEntry = new WeightEntry(
            user.Id,
            request.weight,
            DateOnly.FromDateTime(DateTime.UtcNow));

        await _userRepository.AddAsync(user);
        await _weightEntryRepository.AddAsync(weightEntry);
        await _unitOfWork.SaveChangesAsync();

        return new UserProfileResponse(
            user.Id,
            user.BirthDate,
            user.Gender,
            user.ActivityLevel,
            user.Height,
            user.Allergies,
            user.DietaryPreferences,
            user.SubscriptionTier,
            user.CreatedAt);
    }

    public Task<UserProfileResponse> GetUserProfileAsync(string keycloakId)
        => throw new NotImplementedException();

    public async Task<UserProfileResponse> UpdateUserProfileAsync(
        string keycloakId,
        UpdateUserProfileRequest request)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);

        if (user is null)
            throw new NotFoundException("User profile not found.");

        user.ChangeBirthDate(request.BirthDate);
        user.ChangeGender(request.Gender);
        user.ChangeActivityLevel(request.ActivityLevel);
        user.ChangeHeight(request.Height);
        user.SetListAllergen(request.Allergies);
        user.SetDietaryPreference(request.DietaryPreferences);

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return new UserProfileResponse(
            user.Id,
            user.BirthDate,
            user.Gender,
            user.ActivityLevel,
            user.Height,
            user.Allergies,
            user.DietaryPreferences,
            user.SubscriptionTier,
            user.CreatedAt);
    }

    public Task DeleteUserAsync(string keycloakId)
        => throw new NotImplementedException();

    public Task<UserProfileResponse> ReactivateUserAsync(string keycloakId)
        => throw new NotImplementedException();

    public Task<object> ExportUserDataAsync(string keycloakId)
        => throw new NotImplementedException();

    public Task<WeightEntryResponse> AddWeightEntryAsync(Guid userId, AddWeightEntryRequest request)
        => throw new NotImplementedException();

    public Task<List<WeightEntryResponse>> GetWeightHistoryAsync(Guid userId)
        => throw new NotImplementedException();

    public Task<WeightEntryResponse> UpdateWeightEntryAsync(Guid userId, Guid entryId, UpdateWeightEntryRequest request)
        => throw new NotImplementedException();
}
