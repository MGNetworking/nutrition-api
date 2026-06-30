namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Users;

public interface IRgpdService
{
    /// <summary>Initie la suppression du compte utilisateur.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    Task DeleteUserAsync(string keycloakId);

    /// <summary>Réactive un compte utilisateur pendant la période de grâce.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <returns>Le profil utilisateur réactivé.</returns>
    Task<UserProfileResponse> ReactivateUserAsync(string keycloakId);

    /// <summary>Exporte toutes les données personnelles de l'utilisateur.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <returns>L'archive complète des données utilisateur.</returns>
    Task<UserExportResponse> ExportUserDataAsync(string keycloakId);
}
