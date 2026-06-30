namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Users;

/// <summary>Contrat applicatif pour la gestion du profil utilisateur et des pesées.</summary>
public interface IUserService
{
    /// <summary>Crée le profil d'un utilisateur lors de sa première connexion.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <param name="request">Données du profil à créer.</param>
    /// <returns>Le profil utilisateur créé.</returns>
    Task<UserProfileResponse> CreateUserProfileAsync(string keycloakId, CreateUserProfileRequest request);

    /// <summary>Retourne le profil d'un utilisateur.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <returns>Le profil utilisateur correspondant.</returns>
    Task<UserProfileResponse> GetUserProfileAsync(string keycloakId);

    /// <summary>Met à jour le profil d'un utilisateur existant.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <param name="request">Données mises à jour du profil.</param>
    /// <returns>Le profil utilisateur mis à jour.</returns>
    Task<UserProfileResponse> UpdateUserProfileAsync(string keycloakId, UpdateUserProfileRequest request);

    /// <summary>Ajoute une pesée pour un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="request">Données de la pesée.</param>
    /// <returns>La pesée créée.</returns>
    Task<WeightEntryResponse> AddWeightEntryAsync(Guid userId, AddWeightEntryRequest request);

    /// <summary>Retourne l'historique des pesées d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des pesées de l'utilisateur.</returns>
    Task<List<WeightEntryResponse>> GetWeightHistoryAsync(Guid userId);

    /// <summary>Met à jour une pesée existante.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="entryId">Identifiant de la pesée à modifier.</param>
    /// <param name="request">Données mises à jour de la pesée.</param>
    /// <returns>La pesée mise à jour.</returns>
    Task<WeightEntryResponse> UpdateWeightEntryAsync(Guid userId, Guid entryId, UpdateWeightEntryRequest request);
}
