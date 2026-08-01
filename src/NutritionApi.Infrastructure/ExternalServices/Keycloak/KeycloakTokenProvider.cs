namespace NutritionApi.Infrastructure.ExternalServices.Keycloak;

using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>Obtient et mémorise le jeton de service Keycloak via le flux OAuth2 <c>client_credentials</c>.</summary>
public sealed class KeycloakTokenProvider : IKeycloakTokenProvider
{
    /// <summary>Nom du client HTTP nommé utilisé pour joindre le point de terminaison de jeton.</summary>
    public const string HttpClientName = "keycloak-token";

    /// <summary>
    /// Marge retranchée à la durée de vie du jeton : un jeton qui expire dans moins de 30 secondes est
    /// renouvelé d'avance, afin qu'il ne périme pas entre son obtention et l'appel Admin qui l'utilise.
    /// </summary>
    private static readonly TimeSpan SafetyMargin = TimeSpan.FromSeconds(30);

    private readonly HttpClient _httpClient;
    private readonly KeycloakAdminOptions _options;
    private readonly TimeProvider _timeProvider;

    // Le fournisseur est un singleton partagé par toutes les requêtes : le sémaphore évite que dix
    // appels simultanés déclenchent dix demandes de jeton au démarrage.
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _token;
    private DateTimeOffset _expiresAt;

    /// <summary>Crée le fournisseur de jetons.</summary>
    /// <param name="httpClient">Client HTTP dédié aux appels au point de terminaison de jeton.</param>
    /// <param name="options">Paramètres de connexion à Keycloak.</param>
    /// <param name="timeProvider">Horloge utilisée pour évaluer l'expiration du jeton.</param>
    public KeycloakTokenProvider(HttpClient httpClient, IOptions<KeycloakAdminOptions> options, TimeProvider timeProvider)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    /// <summary>Retourne le jeton mémorisé s'il est encore valide, sinon en demande un nouveau à Keycloak.</summary>
    /// <returns>Le jeton d'accès à présenter en <c>Bearer</c>.</returns>
    /// <exception cref="HttpRequestException">Keycloak a refusé les identifiants du client de service ou est indisponible.</exception>
    /// <exception cref="InvalidOperationException">La réponse de Keycloak ne contient aucun jeton d'accès.</exception>
    public async Task<string> GetTokenAsync()
    {
        if (TryGetCachedToken(out var cached))
            return cached;

        await _gate.WaitAsync();

        try
        {
            // Un appel concurrent a pu renouveler le jeton pendant l'attente du sémaphore.
            if (TryGetCachedToken(out cached))
                return cached;

            var (token, expiresIn) = await RequestTokenAsync();

            _token = token;
            _expiresAt = _timeProvider.GetUtcNow().AddSeconds(expiresIn);

            return token;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Oublie le jeton mémorisé — utilisé lorsqu'un appel Admin se solde par un 401.</summary>
    public void Invalidate() => _token = null;

    /// <summary>Indique si un jeton mémorisé reste utilisable, marge de sécurité comprise.</summary>
    /// <param name="token">Le jeton encore valide, ou une chaîne vide.</param>
    /// <returns><c>true</c> si le jeton peut être réutilisé.</returns>
    private bool TryGetCachedToken(out string token)
    {
        token = _token ?? string.Empty;

        return _token is not null && _timeProvider.GetUtcNow() + SafetyMargin < _expiresAt;
    }

    /// <summary>Demande un nouveau jeton de service au point de terminaison OpenID Connect.</summary>
    /// <returns>Le jeton et sa durée de vie.</returns>
    /// <exception cref="HttpRequestException">Keycloak a répondu par un statut d'erreur.</exception>
    /// <exception cref="InvalidOperationException">La réponse ne contient aucun jeton d'accès.</exception>
    private async Task<(string Token, int ExpiresIn)> RequestTokenAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl}/realms/{_options.Realm}/protocol/openid-connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ServiceClientId,
                ["client_secret"] = _options.ServiceClientSecret
            })
        };

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var payload = JsonSerializer.Deserialize<TokenResponse>(await response.Content.ReadAsStringAsync());

        if (payload?.AccessToken is null)
            throw new InvalidOperationException("Keycloak n'a retourné aucun jeton d'accès pour le client de service.");

        return (payload.AccessToken, payload.ExpiresIn);
    }

    /// <summary>Réponse du point de terminaison de jeton OpenID Connect.</summary>
    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
