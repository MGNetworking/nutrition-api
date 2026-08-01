namespace NutritionApi.Infrastructure.ExternalServices.Keycloak;

/// <summary>Fournit le jeton de service utilisé pour appeler l'API d'administration Keycloak.</summary>
public interface IKeycloakTokenProvider
{
    /// <summary>Retourne un jeton de service valide.</summary>
    /// <returns>Le jeton d'accès à présenter en <c>Bearer</c>.</returns>
    Task<string> GetTokenAsync();

    /// <summary>Écarte le jeton mémorisé — le prochain appel en obtiendra un nouveau.</summary>
    void Invalidate();
}
