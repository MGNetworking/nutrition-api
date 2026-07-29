namespace NutritionApi.Infrastructure.Tests.ExternalServices.Keycloak;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NutritionApi.Application.Exceptions;
using NutritionApi.Infrastructure.ExternalServices.Keycloak;
using System.Net;

public class KeycloakAdminServiceTest
{
    private const string KeycloakId = "9f1b2c3d-4e5f-6789-abcd-ef0123456789";
    private const string UserUrl = "https://keycloak.test/admin/realms/nutrition/users/9f1b2c3d-4e5f-6789-abcd-ef0123456789";

    private readonly Mock<HttpMessageHandler> _handler = new(MockBehavior.Strict);
    private readonly Mock<IKeycloakTokenProvider> _tokenProvider = new(MockBehavior.Strict);
    private readonly Mock<ILogger<KeycloakAdminService>> _logger = new();
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = [];
    private readonly List<string> _bodies = [];

    private static readonly KeycloakAdminOptions AdminOptions = new()
    {
        AdminBaseUrl = "https://keycloak.test",
        Realm = "nutrition",
        ServiceClientId = "nutrition-api-service",
        ServiceClientSecret = "s3cr3t"
    };

    private KeycloakAdminService CreateService()
        => new(new HttpClient(_handler.Object), _tokenProvider.Object, Options.Create(AdminOptions), _logger.Object);

    /// <summary>Branche le handler sur la file de réponses — chaque appel en consomme une.</summary>
    private void GivenHandler()
        => _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                _requests.Add(request);
                _bodies.Add(request.Content?.ReadAsStringAsync().Result ?? string.Empty);
                return Task.FromResult(_responses.Dequeue());
            });

    private void GivenToken(string token = "token-abc")
        => _tokenProvider.Setup(p => p.GetTokenAsync()).ReturnsAsync(token);

    private void GivenResponse(HttpStatusCode status)
        => _responses.Enqueue(new HttpResponseMessage(status) { Content = new StringContent(string.Empty) });

    // ── Chemin nominal ────────────────────────────────────────────────────────

    [Fact]
    public async Task DisableUserAsync_PatchesEnabledToFalse()
    {
        GivenHandler();
        GivenToken();
        GivenResponse(HttpStatusCode.NoContent);

        await CreateService().DisableUserAsync(KeycloakId);

        Assert.Equal(HttpMethod.Patch, _requests[0].Method);
        Assert.Equal(UserUrl, _requests[0].RequestUri!.ToString());
        Assert.Contains("\"enabled\":false", _bodies[0]);
    }

    [Fact]
    public async Task EnableUserAsync_PatchesEnabledToTrue()
    {
        GivenHandler();
        GivenToken();
        GivenResponse(HttpStatusCode.NoContent);

        await CreateService().EnableUserAsync(KeycloakId);

        Assert.Equal(HttpMethod.Patch, _requests[0].Method);
        Assert.Equal(UserUrl, _requests[0].RequestUri!.ToString());
        Assert.Contains("\"enabled\":true", _bodies[0]);
    }

    [Fact]
    public async Task DeleteUserAsync_SendsDeleteToUserEndpoint()
    {
        GivenHandler();
        GivenToken();
        GivenResponse(HttpStatusCode.NoContent);

        await CreateService().DeleteUserAsync(KeycloakId);

        Assert.Equal(HttpMethod.Delete, _requests[0].Method);
        Assert.Equal(UserUrl, _requests[0].RequestUri!.ToString());
    }

    [Fact]
    public async Task DeleteUserAsync_SendsServiceTokenAsBearer()
    {
        GivenHandler();
        GivenToken("token-xyz");
        GivenResponse(HttpStatusCode.NoContent);

        await CreateService().DeleteUserAsync(KeycloakId);

        Assert.Equal("Bearer", _requests[0].Headers.Authorization!.Scheme);
        Assert.Equal("token-xyz", _requests[0].Headers.Authorization!.Parameter);
    }

    // ── Cas limites ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_IgnoresNotFound()
    {
        GivenHandler();
        GivenToken();
        GivenResponse(HttpStatusCode.NotFound);

        await CreateService().DeleteUserAsync(KeycloakId);

        Assert.Single(_requests);
    }

    [Fact]
    public async Task DisableUserAsync_IgnoresNotFound()
    {
        GivenHandler();
        GivenToken();
        GivenResponse(HttpStatusCode.NotFound);

        await CreateService().DisableUserAsync(KeycloakId);

        Assert.Single(_requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteUserAsync_ThrowsWhenKeycloakIdIsBlank(string keycloakId)
        => await Assert.ThrowsAsync<ArgumentException>(() => CreateService().DeleteUserAsync(keycloakId));

    // ── Jeton expiré ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_InvalidatesTokenAndRetriesOnceOnUnauthorized()
    {
        GivenHandler();
        _tokenProvider.SetupSequence(p => p.GetTokenAsync())
            .ReturnsAsync("token-expire")
            .ReturnsAsync("token-frais");
        _tokenProvider.Setup(p => p.Invalidate());
        GivenResponse(HttpStatusCode.Unauthorized);
        GivenResponse(HttpStatusCode.NoContent);

        await CreateService().DeleteUserAsync(KeycloakId);

        _tokenProvider.Verify(p => p.Invalidate(), Times.Once);
        Assert.Equal(2, _requests.Count);
        Assert.Equal("token-frais", _requests[1].Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsWhenUnauthorizedTwice()
    {
        GivenHandler();
        GivenToken();
        _tokenProvider.Setup(p => p.Invalidate());
        GivenResponse(HttpStatusCode.Unauthorized);
        GivenResponse(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateService().DeleteUserAsync(KeycloakId));

        Assert.Equal(2, _requests.Count);
    }

    // ── Cas d'erreur ──────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteUserAsync_ThrowsOnServerError()
    {
        GivenHandler();
        GivenToken();
        GivenResponse(HttpStatusCode.InternalServerError);

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateService().DeleteUserAsync(KeycloakId));
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsServiceUnavailableWhenKeycloakIsUnreachable()
    {
        GivenToken();
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connexion refusée"));

        var exception = await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => CreateService().DeleteUserAsync(KeycloakId));

        Assert.Equal("Keycloak", exception.Dependency);
    }

    [Fact]
    public async Task DeleteUserAsync_ThrowsServiceUnavailableOnTimeout()
    {
        GivenToken();
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("délai dépassé"));

        await Assert.ThrowsAsync<ServiceUnavailableException>(
            () => CreateService().DeleteUserAsync(KeycloakId));
    }

    [Fact]
    public async Task DisableUserAsync_ThrowsOnForbidden()
    {
        GivenHandler();
        GivenToken();
        GivenResponse(HttpStatusCode.Forbidden);

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateService().DisableUserAsync(KeycloakId));
    }
}
