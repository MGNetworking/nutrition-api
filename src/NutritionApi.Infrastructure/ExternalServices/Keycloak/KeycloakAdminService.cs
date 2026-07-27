namespace NutritionApi.Infrastructure.ExternalServices.Keycloak;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NutritionApi.Application.Interfaces.ExternalServices;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

/// <summary>Administre les comptes utilisateurs du realm via l'API d'administration Keycloak.</summary>
public sealed class KeycloakAdminService : IKeycloakAdminService
{
    /// <summary>Nom du client HTTP nommé utilisé pour joindre l'API d'administration.</summary>
    public const string HttpClientName = "keycloak-admin";

    private const string EnabledPayload = """{"enabled":true}""";
    private const string DisabledPayload = """{"enabled":false}""";

    private readonly HttpClient _httpClient;
    private readonly IKeycloakTokenProvider _tokenProvider;
    private readonly KeycloakAdminOptions _options;
    private readonly ILogger<KeycloakAdminService> _logger;

    /// <summary>Crée le service d'administration Keycloak.</summary>
    /// <param name="httpClient">Client HTTP dédié aux appels d'administration.</param>
    /// <param name="tokenProvider">Fournisseur du jeton de service.</param>
    /// <param name="options">Paramètres de connexion à Keycloak.</param>
    /// <param name="logger">Journal des dégradations rencontrées.</param>
    public KeycloakAdminService(
        HttpClient httpClient,
        IKeycloakTokenProvider tokenProvider,
        IOptions<KeycloakAdminOptions> options,
        ILogger<KeycloakAdminService> logger)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>Désactive le compte : Keycloak refuse dès lors tout nouveau login.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <exception cref="ArgumentException">L'identifiant Keycloak est vide.</exception>
    /// <exception cref="HttpRequestException">Keycloak a répondu par un statut d'erreur, hors 404.</exception>
    public Task DisableUserAsync(string keycloakId)
        => SendAsync(HttpMethod.Patch, keycloakId, () => JsonContent(DisabledPayload));

    /// <summary>Réactive le compte : l'utilisateur peut à nouveau se connecter.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <exception cref="ArgumentException">L'identifiant Keycloak est vide.</exception>
    /// <exception cref="HttpRequestException">Keycloak a répondu par un statut d'erreur, hors 404.</exception>
    public Task EnableUserAsync(string keycloakId)
        => SendAsync(HttpMethod.Patch, keycloakId, () => JsonContent(EnabledPayload));

    /// <summary>Supprime définitivement le compte du realm — dernière étape de la purge RGPD.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <exception cref="ArgumentException">L'identifiant Keycloak est vide.</exception>
    /// <exception cref="HttpRequestException">Keycloak a répondu par un statut d'erreur, hors 404.</exception>
    public Task DeleteUserAsync(string keycloakId)
        => SendAsync(HttpMethod.Delete, keycloakId, contentFactory: null);

    /// <summary>
    /// Envoie la requête d'administration, en rejouant une fois après un 401 : le jeton de service a pu
    /// expirer entre son obtention et l'appel. Un 404 signifie que le compte n'existe plus côté Keycloak —
    /// l'opération est alors sans objet et n'est pas une erreur.
    /// </summary>
    /// <param name="method">Verbe HTTP de l'opération.</param>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <param name="contentFactory">Fabrique du corps de requête — rappelée à chaque tentative, un contenu HTTP n'étant consommable qu'une fois.</param>
    /// <exception cref="ArgumentException">L'identifiant Keycloak est vide.</exception>
    /// <exception cref="HttpRequestException">Keycloak a répondu par un statut d'erreur, hors 404.</exception>
    private async Task SendAsync(HttpMethod method, string keycloakId, Func<HttpContent>? contentFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(keycloakId);

        var response = await SendOnceAsync(method, keycloakId, contentFactory);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _tokenProvider.Invalidate();
            response = await SendOnceAsync(method, keycloakId, contentFactory);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning(
                "Compte Keycloak {KeycloakId} introuvable — opération {Method} sans objet.",
                keycloakId,
                method.Method);

            return;
        }

        response.EnsureSuccessStatusCode();
    }

    /// <summary>Exécute une tentative unique, jeton de service à l'appui.</summary>
    /// <param name="method">Verbe HTTP de l'opération.</param>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <param name="contentFactory">Fabrique du corps de requête, ou <c>null</c> pour une requête sans corps.</param>
    /// <returns>La réponse brute de Keycloak, quel que soit son statut.</returns>
    private async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string keycloakId, Func<HttpContent>? contentFactory)
    {
        var request = new HttpRequestMessage(method, $"{_options.BaseUrl}/admin/realms/{_options.Realm}/users/{keycloakId}")
        {
            Content = contentFactory?.Invoke()
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetTokenAsync());

        return await _httpClient.SendAsync(request);
    }

    /// <summary>Emballe une charge utile JSON pour l'API d'administration.</summary>
    /// <param name="payload">Corps de requête sérialisé.</param>
    /// <returns>Le contenu HTTP correspondant.</returns>
    private static StringContent JsonContent(string payload)
        => new(payload, Encoding.UTF8, "application/json");
}
