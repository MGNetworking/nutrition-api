namespace NutritionApi.Api.Tests.Level2;

using Moq;
using NutritionApi.Api.Tests.Level2.Fixtures;
using NutritionApi.Application.Exceptions;
using System.Net;
using System.Net.Http.Json;
using System.Text;

/// <summary>
/// Tests de niveau 2 des composants transverses du pipeline — NTR-105.
/// Couvre la résolution d’utilisateur, la traduction des exceptions, l’authentification et le pipeline.
/// </summary>
/// <remarks>
/// Le rejet d'un jeton expiré n'est pas ici : <c>TestAuthHandler</c> ne vérifie pas l'expiration,
/// il remplace le composant qui s'en charge. Ce cas est couvert au niveau 3, par
/// <c>GetUsersMe_ShouldReturn401_WhenTokenHasExpired</c>.
/// </remarks>
[Trait("Level", "2")]
[Collection(ApiCollection.Name)]
public class MiddlewaresIntegrationTest
{
    private const string PlansEndpoint = "/api/v1/diet-plans";
    private const string AdminEndpoint = "/api/v1/admin/dashboard";

    private readonly ApiFactory _factory;

    public MiddlewaresIntegrationTest(ApiFactory factory) => _factory = factory;

    /// <summary>Arme la lecture des plans pour qu'elle réussisse et retienne l'identifiant reçu.</summary>
    private void GivenPlansReadSucceeds(Action<Guid>? onCalled = null)
        => _factory.DietPlans
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>()))
            .Callback<Guid>(id => onCalled?.Invoke(id))
            .ReturnsAsync([]);

    /// <summary>Arme la lecture des plans pour qu'elle lève l'exception donnée.</summary>
    private void GivenPlansReadThrows(Exception exception)
        => _factory.DietPlans
            .Setup(r => r.GetByUserIdAsync(It.IsAny<Guid>()))
            .ThrowsAsync(exception);

    // ── UserResolutionMiddleware — IT-MW ──────────────────────────────────────

    [Fact]
    public async Task UserResolution_ShouldInjectInternalUserId_WhenIdentityIsValid()
    {
        var recu = Guid.Empty;
        GivenPlansReadSucceeds(id => recu = id);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        // Le controller lit HttpContext.Items["UserId"] : la valeur reçue par le service prouve que
        // le middleware a bien traduit le claim "sub" en identifiant interne.
        Assert.Equal(_factory.CurrentUser.Id, recu);
    }

    [Fact]
    public async Task UserResolution_ShouldReturn401_WhenProfileIsMissing()
    {
        GivenPlansReadSucceeds();

        var reponse = await _factory
            .CreateAuthenticatedClient(subject: "00000000-0000-0000-0000-000000000404")
            .GetAsync(PlansEndpoint);

        // Le jeton est accepté, mais aucun profil ne correspond : le middleware coupe la chaîne.
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task UserResolution_ShouldReturn401BeforeMiddleware_WhenIdentityIsAbsent()
    {
        var reponse = await _factory.CreateAnonymousClient().GetAsync(PlansEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    // ── ExceptionMiddleware — IT-EX ───────────────────────────────────────────

    [Theory]
    [InlineData(typeof(NotFoundException), HttpStatusCode.NotFound)]
    [InlineData(typeof(ConflictException), HttpStatusCode.Conflict)]
    [InlineData(typeof(ForbiddenException), HttpStatusCode.Forbidden)]
    [InlineData(typeof(UnprocessableException), HttpStatusCode.UnprocessableEntity)]
    public async Task ExceptionMiddleware_ShouldTranslateToHttpStatus_WhenApplicationExceptionIsThrown(Type type, HttpStatusCode attendu)
    {
        GivenPlansReadThrows((Exception)Activator.CreateInstance(type, "message de test")!);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);

        Assert.Equal(attendu, reponse.StatusCode);
    }

    [Fact]
    public async Task ExceptionMiddleware_ShouldReturnProblemDetailsWithMessage_WhenNotFoundIsThrown()
    {
        GivenPlansReadThrows(new NotFoundException("plan introuvable"));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);
        var corps = await reponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        Assert.Contains("plan introuvable", corps);
    }

    [Fact]
    public async Task ExceptionMiddleware_ShouldReturn500WithoutDetails_WhenExceptionIsUnhandled()
    {
        GivenPlansReadThrows(new InvalidOperationException("chaîne de connexion invalide"));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);
        var corps = await reponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, reponse.StatusCode);

        // Le message d'origine ne doit pas franchir la frontière HTTP : il peut porter des détails
        // d'infrastructure.
        Assert.DoesNotContain("chaîne de connexion", corps);
    }

    // ── Invariants de domaine et dépendances — NTR-135 ────────────────────────

    [Fact]
    public async Task InvariantDeDomaineViole_Retourne422()
    {
        GivenPlansReadThrows(new ArgumentException("Macro percentages must sum to 100."));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);
        var corps = await reponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);

        // Le message vient du domaine et décrit la règle violée : il est utile au client.
        Assert.Contains("sum to 100", corps);
    }

    [Fact]
    public async Task DependanceIndisponible_Retourne503AvecRetryAfter()
    {
        GivenPlansReadThrows(new ServiceUnavailableException("PostgreSQL"));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);
        var corps = await reponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, reponse.StatusCode);
        Assert.NotNull(reponse.Headers.RetryAfter);

        // Le nom de la dépendance appartient au journal, pas à la réponse.
        Assert.DoesNotContain("PostgreSQL", corps);
    }

    [Fact]
    public async Task ToutProblemDetails_PorteUnTraceId()
    {
        GivenPlansReadThrows(new NotFoundException("introuvable"));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);
        var corps = await reponse.Content.ReadAsStringAsync();

        // Le lien entre le signalement d'un utilisateur et la trace serveur.
        Assert.Contains("traceId", corps);
        Assert.Contains("\"title\"", corps);
        Assert.Contains("\"type\"", corps);
    }

    // ── Authentification et autorisation — IT-AUTH ────────────────────────────

    [Fact]
    public async Task Authentication_ShouldReturn401_WhenTokenIsAbsent()
    {
        var reponse = await _factory.CreateAnonymousClient().GetAsync(AdminEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task Authorization_ShouldReturn403_WhenRoleIsInsufficient()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(AdminEndpoint);

        // Identité valide, mais la policy AdminOnly exige le rôle admin.
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task Authorization_ShouldAllow_WhenRoleIsAdmin()
    {
        var reponse = await _factory.CreateAuthenticatedClient(roles: "admin").GetAsync(AdminEndpoint);

        Assert.NotEqual(HttpStatusCode.Unauthorized, reponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // ── Pipeline MVC — IT-PIPE ────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_ShouldReturn400_WhenJsonBodyIsInvalid()
    {
        var contenu = new StringContent("{ ceci n'est pas du json }", Encoding.UTF8, "application/json");

        var reponse = await _factory.CreateAuthenticatedClient().PostAsync(PlansEndpoint, contenu);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task Pipeline_ShouldReturn404_WhenRouteDoesNotExist()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync("/api/v1/route-qui-nexiste-pas");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task Pipeline_ShouldReturn404_WhenGuidIsMalformed()
    {
        var contenu = JsonContent.Create(new { });

        var reponse = await _factory.CreateAuthenticatedClient().PutAsync($"{PlansEndpoint}/pas-un-guid", contenu);

        // La contrainte {id:guid} fait partie du filtrage de route : une valeur non conforme
        // empêche la route de correspondre, ce qui donne 404 et non 400. Le recensement annonce
        // 400 — écart signalé, le comportement réel d'ASP.NET Core fait foi.
        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }
}
