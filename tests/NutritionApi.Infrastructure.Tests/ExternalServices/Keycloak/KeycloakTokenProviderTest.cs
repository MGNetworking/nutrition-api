namespace NutritionApi.Infrastructure.Tests.ExternalServices.Keycloak;

using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NutritionApi.Infrastructure.ExternalServices.Keycloak;
using System.Net;
using System.Text;

public class KeycloakTokenProviderTest
{
    private const string TokenUrl = "https://keycloak.test/realms/nutrition/protocol/openid-connect/token";

    private readonly Mock<HttpMessageHandler> _handler = new(MockBehavior.Strict);
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = [];
    private readonly List<string> _bodies = [];
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private static readonly KeycloakAdminOptions AdminOptions = new()
    {
        AdminBaseUrl = "https://keycloak.test",
        Realm = "nutrition",
        ServiceClientId = "nutrition-api-service",
        ServiceClientSecret = "s3cr3t"
    };

    private KeycloakTokenProvider CreateProvider()
        => new(new HttpClient(_handler.Object), Options.Create(AdminOptions), _time);

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

    private void GivenToken(string accessToken, int expiresIn = 300)
        => _responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"{{accessToken}}","expires_in":{{expiresIn}},"token_type":"Bearer"}""",
                Encoding.UTF8,
                "application/json")
        });

    private void GivenFailure(HttpStatusCode status)
        => _responses.Enqueue(new HttpResponseMessage(status) { Content = new StringContent("{}") });

    // ── Chemin nominal ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_ReturnsAccessTokenFromKeycloak()
    {
        GivenHandler();
        GivenToken("token-abc");

        var token = await CreateProvider().GetTokenAsync();

        Assert.Equal("token-abc", token);
    }

    [Fact]
    public async Task GetTokenAsync_PostsClientCredentialsToTokenEndpoint()
    {
        GivenHandler();
        GivenToken("token-abc");

        await CreateProvider().GetTokenAsync();

        Assert.Equal(HttpMethod.Post, _requests[0].Method);
        Assert.Equal(TokenUrl, _requests[0].RequestUri!.ToString());
        Assert.Contains("grant_type=client_credentials", _bodies[0]);
        Assert.Contains("client_id=nutrition-api-service", _bodies[0]);
        Assert.Contains("client_secret=s3cr3t", _bodies[0]);
    }

    // ── Cache ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_ReusesCachedTokenWhenStillValid()
    {
        GivenHandler();
        GivenToken("token-abc", expiresIn: 300);
        var provider = CreateProvider();

        await provider.GetTokenAsync();
        _time.Advance(TimeSpan.FromSeconds(100));
        var second = await provider.GetTokenAsync();

        Assert.Equal("token-abc", second);
        Assert.Single(_requests);
    }

    [Fact]
    public async Task GetTokenAsync_RequestsNewTokenAfterExpiration()
    {
        GivenHandler();
        GivenToken("token-abc", expiresIn: 300);
        GivenToken("token-def", expiresIn: 300);
        var provider = CreateProvider();

        await provider.GetTokenAsync();
        _time.Advance(TimeSpan.FromSeconds(301));
        var second = await provider.GetTokenAsync();

        Assert.Equal("token-def", second);
        Assert.Equal(2, _requests.Count);
    }

    [Fact]
    public async Task GetTokenAsync_RenewsBeforeExpirationWithinSafetyMargin()
    {
        GivenHandler();
        GivenToken("token-abc", expiresIn: 60);
        GivenToken("token-def", expiresIn: 60);
        var provider = CreateProvider();

        await provider.GetTokenAsync();

        // 45 s : le jeton est encore techniquement valide, mais entre dans la marge de sécurité de 30 s.
        _time.Advance(TimeSpan.FromSeconds(45));
        var second = await provider.GetTokenAsync();

        Assert.Equal("token-def", second);
        Assert.Equal(2, _requests.Count);
    }

    [Fact]
    public async Task Invalidate_ForcesNewTokenOnNextCall()
    {
        GivenHandler();
        GivenToken("token-abc");
        GivenToken("token-def");
        var provider = CreateProvider();

        await provider.GetTokenAsync();
        provider.Invalidate();
        var second = await provider.GetTokenAsync();

        Assert.Equal("token-def", second);
        Assert.Equal(2, _requests.Count);
    }

    // ── Concurrence ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_RequestsTokenOnceUnderConcurrentCalls()
    {
        GivenHandler();
        GivenToken("token-abc");
        var provider = CreateProvider();

        var results = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => provider.GetTokenAsync()));

        Assert.All(results, t => Assert.Equal("token-abc", t));
        Assert.Single(_requests);
    }

    // ── Cas d'erreur ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTokenAsync_ThrowsWhenKeycloakRejectsCredentials()
    {
        GivenHandler();
        GivenFailure(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAsync<HttpRequestException>(() => CreateProvider().GetTokenAsync());
    }

    [Fact]
    public async Task GetTokenAsync_ThrowsWhenResponseHasNoAccessToken()
    {
        GivenHandler();
        _responses.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"expires_in":300}""", Encoding.UTF8, "application/json")
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateProvider().GetTokenAsync());
    }

    [Fact]
    public async Task GetTokenAsync_RetriesAfterFailure()
    {
        GivenHandler();
        GivenFailure(HttpStatusCode.ServiceUnavailable);
        GivenToken("token-abc");
        var provider = CreateProvider();

        await Assert.ThrowsAsync<HttpRequestException>(() => provider.GetTokenAsync());
        var token = await provider.GetTokenAsync();

        Assert.Equal("token-abc", token);
    }

    /// <summary>Horloge pilotée par le test — évite d'attendre réellement l'expiration d'un jeton.</summary>
    private sealed class MutableTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now = _now.Add(delta);
    }
}
