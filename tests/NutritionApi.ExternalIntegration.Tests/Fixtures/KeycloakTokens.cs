namespace NutritionApi.ExternalIntegration.Tests.Fixtures;

using System.Net;
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
    // Toutes les valeurs ci-dessous sont lues dans keycloak/realm-export.json, jamais ressaisies.
    // Un renommage dans l'export fait échouer le test avec un message qui nomme le compte manquant,
    // au lieu de le laisser interroger un identifiant qui n'existe plus.

    /// <summary>Compte porteur du seul rôle <c>user</c>.</summary>
    public const string StandardUser = "test-user";

    /// <summary>Compte porteur des rôles <c>user</c> et <c>admin</c>.</summary>
    public const string AdminUser = "test-admin";

    /// <summary>Client public de l'application — celui dont les jetons sont acceptés par l'API.</summary>
    public static string DefaultClient => RealmExport.RequireClient("nutrition-api");

    /// <summary>Client confidentiel utilisé par l'API pour agir sur le realm.</summary>
    public static string ServiceClient => RealmExport.RequireClient("nutrition-api-service");

    /// <summary>
    /// Client de test émettant des jetons valides une seconde. Le realm impose 1800 s : sans lui,
    /// le test d'expiration devrait attendre trente minutes.
    /// </summary>
    public static string ShortLivedClient => RealmExport.RequireClient("nutrition-api-tests-shortlived");

    /// <summary>
    /// Client de test sans mapper d'audience — ses jetons ne portent pas <c>nutrition-api</c> dans
    /// leur claim <c>aud</c>, et l'API doit donc les refuser.
    /// </summary>
    public static string NoAudienceClient => RealmExport.RequireClient("nutrition-api-tests-no-audience");

    /// <summary>Nom du realm.</summary>
    public static string Realm => RealmExport.RealmName;

    /// <summary>Secret du client de service.</summary>
    public static string ServiceClientSecret => RealmExport.SecretOf(ServiceClient);

    /// <summary>Identifiant Keycloak de <see cref="StandardUser"/>.</summary>
    public static string StandardUserSubject => RealmExport.SubjectOf(StandardUser);

    /// <summary>Identifiant Keycloak de <see cref="AdminUser"/>.</summary>
    public static string AdminUserSubject => RealmExport.SubjectOf(AdminUser);

    /// <summary>Racine du serveur Keycloak — surchargeable par <c>NUTRITION_TEST_KEYCLOAK_URL</c> en CI.</summary>
    public static string AdminBaseUrl =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_KEYCLOAK_URL") ?? "http://localhost:8778";

    /// <summary>Autorité du realm — surchargeable par <c>NUTRITION_TEST_KEYCLOAK</c> en CI.</summary>
    public static string Authority =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_KEYCLOAK")
        ?? $"{AdminBaseUrl}/realms/{Realm}";

    private readonly HttpClient _client = new();

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
            ["password"] = RealmExport.PasswordOf(username),
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

    /// <summary>
    /// Crée un compte jetable dans le realm et retourne son identifiant Keycloak.
    /// </summary>
    /// <param name="username">Nom du compte — unique, sinon Keycloak répond 409.</param>
    /// <returns>L'identifiant du compte créé, celui qui alimente <c>User.KeycloakId</c>.</returns>
    /// <remarks>
    /// Un test qui inventerait cet identifiant construirait un état impossible : la colonne
    /// <c>keycloak_id</c> est, par définition, le <c>sub</c> d'un compte existant. La purge RGPD
    /// emprunterait alors la branche « compte déjà absent » — un 404 traité comme succès — au lieu
    /// de la branche nominale.
    /// <para>
    /// Le compte est créé avec le service account de l'API, qui porte déjà le rôle
    /// <c>manage-users</c> nécessaire à la purge.
    /// </para>
    /// </remarks>
    public async Task<string> CreateThrowawayUserAsync(string username)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Post, $"{AdminBaseUrl}/admin/realms/{Realm}/users")
        {
            Content = JsonContent.Create(new { username, enabled = true }),
        };

        requete.Headers.Authorization = new("Bearer", await GetServiceTokenAsync());

        var reponse = await _client.SendAsync(requete);

        if (!reponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Création du compte « {username} » refusée : "
                + $"{(int)reponse.StatusCode} {await reponse.Content.ReadAsStringAsync()}");
        }

        // Keycloak ne renvoie pas le compte créé : son identifiant est le dernier segment de Location.
        var emplacement = reponse.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("Keycloak n'a pas renvoyé l'emplacement du compte créé.");

        return emplacement[(emplacement.LastIndexOf('/') + 1)..];
    }

    /// <summary>Indique si un compte existe encore dans le realm.</summary>
    /// <param name="keycloakId">Identifiant du compte.</param>
    /// <returns><c>true</c> tant que Keycloak le connaît.</returns>
    public async Task<bool> UserExistsAsync(string keycloakId)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Get, $"{AdminBaseUrl}/admin/realms/{Realm}/users/{keycloakId}");
        requete.Headers.Authorization = new("Bearer", await GetServiceTokenAsync());

        var reponse = await _client.SendAsync(requete);

        return reponse.StatusCode != HttpStatusCode.NotFound;
    }

    /// <summary>Supprime un compte du realm — sans effet s'il n'existe plus.</summary>
    /// <param name="keycloakId">Identifiant du compte.</param>
    /// <remarks>Filet de sécurité : un test qui échoue avant la purge ne doit pas laisser de compte derrière lui.</remarks>
    public async Task DeleteUserAsync(string keycloakId)
    {
        using var requete = new HttpRequestMessage(HttpMethod.Delete, $"{AdminBaseUrl}/admin/realms/{Realm}/users/{keycloakId}");
        requete.Headers.Authorization = new("Bearer", await GetServiceTokenAsync());

        await _client.SendAsync(requete);
    }

    /// <summary>Obtient un jeton d'administration par le flux <c>client_credentials</c>.</summary>
    /// <returns>Le jeton du service account <c>nutrition-api-service</c>.</returns>
    private async Task<string> GetServiceTokenAsync()
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = ServiceClient,
            ["client_secret"] = ServiceClientSecret,
        });

        var reponse = await _client.PostAsync($"{Authority}/protocol/openid-connect/token", form);

        if (!reponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Jeton de service refusé : {(int)reponse.StatusCode} {await reponse.Content.ReadAsStringAsync()}");
        }

        return (await reponse.Content.ReadFromJsonAsync<TokenResponse>())?.AccessToken
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
