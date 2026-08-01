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

    /// <summary>Crée le profil utilisateur et persiste la pesée initiale.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <param name="request">Données du profil et poids initial.</param>
    /// <returns>Le profil utilisateur créé.</returns>
    /// <exception cref="ConflictException">Un profil existe déjà pour cet identifiant Keycloak.</exception>
    public async Task<UserProfileResponse> CreateUserProfileAsync(
        string keycloakId,
        CreateUserProfileRequest request)
    {
        await ThrowIfUserProfileExistsAsync(keycloakId);

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

    /// <summary>Retourne le profil d'un utilisateur.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <returns>Le profil utilisateur correspondant.</returns>
    /// <exception cref="NotFoundException">Aucun profil trouvé pour cet identifiant Keycloak.</exception>
    public async Task<UserProfileResponse> GetUserProfileAsync(string keycloakId)
    {
        var user = await GetUserByKeycloakIdOrThrowAsync(keycloakId);
        return UserProfileResponse.From(user);
    }

    /// <summary>Met à jour le profil d'un utilisateur existant.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <param name="request">Données mises à jour du profil.</param>
    /// <returns>Le profil utilisateur mis à jour.</returns>
    /// <exception cref="NotFoundException">Aucun profil trouvé pour cet identifiant Keycloak.</exception>
    public async Task<UserProfileResponse> UpdateUserProfileAsync(
        string keycloakId,
        UpdateUserProfileRequest request)
    {
        var user = await GetUserByKeycloakIdOrThrowAsync(keycloakId);

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

    /// <summary>Ajoute une pesée pour un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="request">Données de la pesée.</param>
    /// <returns>La pesée créée.</returns>
    /// <exception cref="NotFoundException">L'utilisateur n'existe pas.</exception>
    /// <exception cref="ConflictException">Une pesée existe déjà pour cette date.</exception>
    public async Task<WeightEntryResponse> AddWeightEntryAsync(Guid userId, AddWeightEntryRequest request)
    {
        var user = await GetUserIdOrThrowAsync(userId);

        // La date effective est résolue avant le contrôle, et non après : conditionner celui-ci à
        // MeasuredAt laissait passer les appels sans date, qui créaient un doublon silencieux sur
        // la journée courante. La contrainte d'unicité en base ferme définitivement ce chemin ;
        // ce contrôle reste pour renvoyer un 409 explicite plutôt qu'une erreur de persistance.
        var measuredAt = request.MeasuredAt ?? DateOnly.FromDateTime(DateTime.UtcNow);

        var existing = await _weightEntryRepository.GetByUserIdAndDateAsync(userId, measuredAt);
        if (existing is not null)
            throw new ConflictException("A weight entry already exists for this date.");

        var weightEntry = new WeightEntry(userId, request.Weight, measuredAt);

        await _weightEntryRepository.AddAsync(weightEntry);
        await _unitOfWork.SaveChangesAsync();
        return WeightEntryResponse.From(weightEntry);
    }

    /// <summary>Retourne l'historique des pesées d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des pesées de l'utilisateur.</returns>
    /// <exception cref="NotFoundException">L'utilisateur n'existe pas.</exception>
    public async Task<List<WeightEntryResponse>> GetWeightHistoryAsync(Guid userId)
    {
        var user = await GetUserIdOrThrowAsync(userId);

        var weightEntries = await _weightEntryRepository.GetByUserIdAsync(userId);
        return weightEntries.Select(WeightEntryResponse.From).ToList();
    }

    /// <summary>Met à jour une pesée existante.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="entryId">Identifiant de la pesée à modifier.</param>
    /// <param name="request">Données mises à jour de la pesée.</param>
    /// <returns>La pesée mise à jour.</returns>
    /// <exception cref="NotFoundException">L'utilisateur ou la pesée n'existe pas.</exception>
    public async Task<WeightEntryResponse> UpdateWeightEntryAsync(Guid userId, Guid entryId, UpdateWeightEntryRequest request)
    {
        var user = await GetUserIdOrThrowAsync(userId);
        var weightEntry = await _weightEntryRepository.GetByIdAsync(entryId);

        if (weightEntry is null || weightEntry.UserId != userId)
            throw new NotFoundException("Weight entry not found.");

        weightEntry.Update(request.Weight, request.MeasuredAt);

        await _weightEntryRepository.UpdateAsync(weightEntry);
        await _unitOfWork.SaveChangesAsync();
        return WeightEntryResponse.From(weightEntry);
    }

    /// <summary>Lève <see cref="ConflictException"/> si un profil existe déjà pour cet identifiant Keycloak.</summary>
    private async Task ThrowIfUserProfileExistsAsync(string keycloakId)
    {
        var userExisting = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (userExisting is not null)
            throw new ConflictException("User profile already exists.");
    }

    /// <summary>Retourne l'utilisateur ou lève <see cref="NotFoundException"/> s'il n'existe pas.</summary>
    private async Task<User> GetUserByKeycloakIdOrThrowAsync(string keycloakId)
    {
        var userExisting = await _userRepository.GetByKeycloakIdAsync(keycloakId);
        if (userExisting is null)
            throw new NotFoundException("User profile not found.");
        return userExisting;
    }

    /// <summary>Retourne l'utilisateur ou lève <see cref="NotFoundException"/> s'il n'existe pas.</summary>
    private async Task<User> GetUserIdOrThrowAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User profile not found.");
        return user;
    }
}
