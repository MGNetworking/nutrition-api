namespace NutritionApi.ExternalIntegration.Tests.Fixtures;

using System.Net.Http.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Obtient de vrais jetons d'accès auprès du Keycloak du <c>docker-compose.yml</c>.
/// </summary>
/// <remarks>
/// C'est ce qui distingue ce niveau du niveau 2 : là-bas, <c>TestAuthHandler</c> fabrique une
/// identité sans jeton, donc sans issuer, sans audience et sans signature à valider. Ici le jeton
/// est émis par Keycloak et traverse la validation réelle de l'API.
/// <para>
/// Le client <c>nutrition-api</c> est public et autorise le <i>direct access grant</i> : le mot de
/// passe suffit, aucun secret client n'est nécessaire. Son mapper d'audience ajoute
/// <c>nutrition-api</c> au jeton, ce que l'API exige.
/// </para>
/// </remarks>
public sealed class KeycloakTokens : IDisposable
{
    /// <summary>Comptes du realm importé — mot de passe commun, voir <c>keycloak/realm-export.json</c>.</summary>
    public const string Password = "test";

    /// <summary>Compte porteur du seul rôle <c>user</c>.</summary>
    public const string StandardUser = "test-user";

    /// <summary>Compte porteur des rôles <c>user</c> et <c>admin</c>.</summary>
    public const string AdminUser = "test-admin";

    /// <summary>Identifiant Keycloak de <see cref="StandardUser"/> — figé dans l'export du realm.</summary>
    public const string StandardUserSubject = "11111111-0000-0000-0000-000000000001";

    /// <summary>Identifiant Keycloak de <see cref="AdminUser"/> — figé dans l'export du realm.</summary>
    public const string AdminUserSubject = "11111111-0000-0000-0000-000000000003";

    /// <summary>Nom du realm importé au démarrage de Keycloak.</summary>
    public const string Realm = "nutrition";

    /// <summary>
    /// Secret du client de service <c>nutrition-api-service</c>, tel que déclaré dans
    /// <c>keycloak/realm-export.json</c>.
    /// </summary>
    /// <remarks>
    /// En clair volontairement : ce realm de développement est jeté et réimporté à chaque recréation
    /// du conteneur. Hors développement, la valeur vient d'une variable d'environnement.
    /// </remarks>
    public const string ServiceClientSecret = "dev-service-secret";

    /// <summary>Racine du serveur Keycloak — surchargeable par <c>NUTRITION_TEST_KEYCLOAK_URL</c> en CI.</summary>
    public static string AdminBaseUrl =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_KEYCLOAK_URL") ?? "http://localhost:8778";

    /// <summary>Autorité du realm — surchargeable par <c>NUTRITION_TEST_KEYCLOAK</c> en CI.</summary>
    public static string Authority =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_KEYCLOAK")
        ?? $"{AdminBaseUrl}/realms/{Realm}";

    private readonly HttpClient _client = new();

    /// <summary>Client public de l'application — celui dont les jetons sont acceptés par l'API.</summary>
    public const string DefaultClient = "nutrition-api";

    /// <summary>
    /// Client de test émettant des jetons valides une seconde. Le realm impose 1800 s : sans lui,
    /// IT-EXT-18 devrait attendre trente minutes.
    /// </summary>
    public const string ShortLivedClient = "nutrition-api-tests-shortlived";

    /// <summary>
    /// Client de test sans mapper d'audience — ses jetons ne portent pas <c>nutrition-api</c> dans
    /// leur claim <c>aud</c>, et l'API doit donc les refuser.
    /// </summary>
    public const string NoAudienceClient = "nutrition-api-tests-no-audience";

    /// <summary>Récupère un jeton d'accès pour un compte du realm.</summary>
    /// <param name="username">Nom du compte, par exemple <see cref="AdminUser"/>.</param>
    /// <param name="clientId">Client émetteur — <see cref="DefaultClient"/> par défaut.</param>
    /// <returns>Le jeton d'accès brut, à porter en <c>Authorization: Bearer</c>.</returns>
    /// <exception cref="InvalidOperationException">Keycloak est injoignable ou a refusé la demande.</exception>
    public async Task<string> GetAccessTokenAsync(string username, string? clientId = null)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = clientId ?? DefaultClient,
            ["username"] = username,
            ["password"] = Password,
        });

        HttpResponseMessage response;

        try
        {
            response = await _client.PostAsync($"{Authority}/protocol/openid-connect/token", form);
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException(
                $"Keycloak est injoignable sur {Authority}. "
                + "Monter la pile avec ./scripts/dev-up.sh avant de lancer les tests de niveau 3.",
                exception);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Keycloak a refusé la demande de jeton pour « {username} » : "
                + $"{(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();

        return payload?.AccessToken
            ?? throw new InvalidOperationException("La réponse de Keycloak ne contient pas de jeton d'accès.");
    }

    /// <inheritdoc />
    public void Dispose() => _client.Dispose();

    private sealed record TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; init; }
    }
}
