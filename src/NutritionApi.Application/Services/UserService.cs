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

        return UserProfileResponse.From(user);
    }

    public async Task<UserProfileResponse> GetUserProfileAsync(string keycloakId)
    {
        var user = await _userRepository.GetByKeycloakIdAsync(keycloakId);

        if (user is null)
            throw new NotFoundException("User profile not found.");

        return UserProfileResponse.From(user);
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

        return UserProfileResponse.From(user);
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

        return UserProfileResponse.From(user);
    }

    public async Task<WeightEntryResponse> AddWeightEntryAsync(Guid userId, AddWeightEntryRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User profile not found.");

        // Une seul mesure de poids par jour, on vérifie si une entrée existe déjà pour la date donnée
        if (request.MeasuredAt is not null)
        {
            var existing = await _weightEntryRepository.GetByUserIdAndDateAsync(userId, request.MeasuredAt.Value);
            if (existing is not null)
                throw new ConflictException("A weight entry already exists for this date.");
        }

        var weightEntry = new WeightEntry(
            userId,
            request.Weight,
            request.MeasuredAt ?? DateOnly.FromDateTime(DateTime.UtcNow));

        await _weightEntryRepository.AddAsync(weightEntry);
        await _unitOfWork.SaveChangesAsync();
        return WeightEntryResponse.From(weightEntry);
    }

    public async Task<List<WeightEntryResponse>> GetWeightHistoryAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User profile not found.");

        var weightEntries = await _weightEntryRepository.GetByUserIdAsync(userId);
        return weightEntries.Select(WeightEntryResponse.From).ToList();
    }

    public async Task<WeightEntryResponse> UpdateWeightEntryAsync(Guid userId, Guid entryId, UpdateWeightEntryRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User profile not found.");

        var weightEntry = await _weightEntryRepository.GetByIdAsync(entryId);
        if (weightEntry is null || weightEntry.UserId != userId)
            throw new NotFoundException("Weight entry not found.");

        weightEntry.Update(request.Weight, request.MeasuredAt);

        await _weightEntryRepository.UpdateAsync(weightEntry);
        await _unitOfWork.SaveChangesAsync();
        return WeightEntryResponse.From(weightEntry);
    }
}
