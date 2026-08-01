namespace NutritionApi.Infrastructure.ExternalServices.Keycloak;

/// <summary>Paramètres de connexion à l'API d'administration Keycloak — section <c>Keycloak</c>.</summary>
public sealed class KeycloakAdminOptions
{
    /// <summary>Nom de la section de configuration liée à ces options.</summary>
    public const string SectionName = "Keycloak";

    /// <summary>URL racine du serveur Keycloak, sans slash final.</summary>
    public string AdminBaseUrl { get; set; } = string.Empty;

    /// <summary>Realm hébergeant les comptes utilisateurs de l'application.</summary>
    public string Realm { get; set; } = string.Empty;

    /// <summary>Identifiant du client de service utilisé pour le flux <c>client_credentials</c>.</summary>
    public string ServiceClientId { get; set; } = string.Empty;

    /// <summary>Secret du client de service — injecté par variable d'environnement, jamais versionné.</summary>
    public string ServiceClientSecret { get; set; } = string.Empty;

    /// <summary>Racine des URLs Keycloak, débarrassée de son slash final.</summary>
    public string BaseUrl => AdminBaseUrl.TrimEnd('/');
}
