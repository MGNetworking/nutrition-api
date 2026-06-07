

namespace NutritionApi.Application.Services;

using Interfaces;
using Interfaces.Repositories;
using Interfaces.Services;
using NutritionApi.Application.DTOS.Users;
using static NutritionApi.Application.DTOs.Users.UserMappingDto;
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

        return UserToUserProfileResponse(user);
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(string keycloakId)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);

        if (user is null)
            throw new NotFoundException("User profile not found.");

        return UserToUserProfileResponse(user);
    }

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

        return UserToUserProfileResponse(user);
    }

    public async Task DeleteUserAsync(string keycloakId)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);

        if (user is null)
            throw new NotFoundException("User profile not found.");

        user.MarkAsDeleted(); // c'est un soft delete

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<UserProfileResponse> ReactivateUserAsync(string keycloakId)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);

        if (user is null)
            throw new NotFoundException("User profile not found.");

        user.Reactivate(); // c'est une reactivation soft 

        await _userRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        return UserToUserProfileResponse(user) as UserProfileResponse;
    }

    public async Task<WeightEntryResponse> AddWeightEntryAsync(Guid userId, AddWeightEntryRequest request)
        => throw new NotImplementedException();

    public async Task<List<WeightEntryResponse>> GetWeightHistoryAsync(Guid userId)
        => throw new NotImplementedException();

    public async Task<WeightEntryResponse> UpdateWeightEntryAsync(Guid userId, Guid entryId, UpdateWeightEntryRequest request)
        => throw new NotImplementedException();
}
