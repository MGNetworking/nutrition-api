namespace NutritionApi.Application.Interfaces.ExternalServices;

/// <summary>Contrat d'administration des comptes utilisateurs dans Keycloak.</summary>
public interface IKeycloakAdminService
{
    /// <summary>Désactive un compte utilisateur dans Keycloak.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    Task DisableUserAsync(string keycloakId);

    /// <summary>Réactive un compte utilisateur dans Keycloak.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    Task EnableUserAsync(string keycloakId);

    /// <summary>Supprime définitivement un compte utilisateur dans Keycloak.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    Task DeleteUserAsync(string keycloakId);
}
