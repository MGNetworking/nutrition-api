namespace NutritionApi.Api.Tests.Integration;

using Moq;
using NutritionApi.Api.Tests.Integration.Fixtures;
using NutritionApi.Application.Exceptions;
using System.Net;
using System.Net.Http.Json;
using System.Text;

/// <summary>
/// Tests de niveau 2 des composants transverses du pipeline — NTR-105.
/// Couvre IT-MW-*, IT-EX-*, IT-AUTH-01/03/04 et IT-PIPE-*.
/// </summary>
/// <remarks>
/// IT-AUTH-02 (jeton expiré) n'est pas ici : <c>TestAuthHandler</c> ne vérifie pas l'expiration,
/// il remplace le composant qui s'en charge. Ce cas relève du niveau 3 (NTR-28).
/// </remarks>
public class MiddlewaresIntegrationTest : IClassFixture<ApiFactory>
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
    public async Task IT_MW_01_IdentiteValide_InjecteLUserIdInterneDansLeControleur()
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
    public async Task IT_MW_02_IdentiteValideMaisProfilAbsent_Retourne401()
    {
        GivenPlansReadSucceeds();

        var reponse = await _factory
            .CreateAuthenticatedClient(subject: "00000000-0000-0000-0000-000000000404")
            .GetAsync(PlansEndpoint);

        // Le jeton est accepté, mais aucun profil ne correspond : le middleware coupe la chaîne.
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_MW_03_SansIdentite_Retourne401AvantLeMiddleware()
    {
        var reponse = await _factory.CreateAnonymousClient().GetAsync(PlansEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    // ── ExceptionMiddleware — IT-EX ───────────────────────────────────────────

    [Theory]
    [InlineData(typeof(NotFoundException), HttpStatusCode.NotFound)]          // IT-EX-01
    [InlineData(typeof(ConflictException), HttpStatusCode.Conflict)]          // IT-EX-02
    [InlineData(typeof(ForbiddenException), HttpStatusCode.Forbidden)]        // IT-EX-03
    [InlineData(typeof(UnprocessableException), HttpStatusCode.UnprocessableEntity)] // IT-EX-04
    public async Task IT_EX_01_a_04_ExceptionApplicative_EstTraduiteEnStatutHttp(Type type, HttpStatusCode attendu)
    {
        GivenPlansReadThrows((Exception)Activator.CreateInstance(type, "message de test")!);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);

        Assert.Equal(attendu, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_EX_01_NotFound_RetourneUnProblemDetailsPortantLeMessage()
    {
        GivenPlansReadThrows(new NotFoundException("plan introuvable"));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);
        var corps = await reponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        Assert.Contains("plan introuvable", corps);
    }

    [Fact]
    public async Task IT_EX_05_ExceptionNonGeree_Retourne500SansDivulguerLeDetail()
    {
        GivenPlansReadThrows(new InvalidOperationException("chaîne de connexion invalide"));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(PlansEndpoint);
        var corps = await reponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, reponse.StatusCode);

        // Le message d'origine ne doit pas franchir la frontière HTTP : il peut porter des détails
        // d'infrastructure.
        Assert.DoesNotContain("chaîne de connexion", corps);
    }

    // ── Authentification et autorisation — IT-AUTH ────────────────────────────

    [Fact]
    public async Task IT_AUTH_01_SansIdentite_Retourne401()
    {
        var reponse = await _factory.CreateAnonymousClient().GetAsync(AdminEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_AUTH_03_RoleInsuffisant_Retourne403()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(AdminEndpoint);

        // Identité valide, mais la policy AdminOnly exige le rôle admin.
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_AUTH_04_RoleAdmin_NEstNiRefuseNiInterdit()
    {
        var reponse = await _factory.CreateAuthenticatedClient(roles: "admin").GetAsync(AdminEndpoint);

        Assert.NotEqual(HttpStatusCode.Unauthorized, reponse.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // ── Pipeline MVC — IT-PIPE ────────────────────────────────────────────────

    [Fact]
    public async Task IT_PIPE_01_BodyJsonInvalide_Retourne400()
    {
        var contenu = new StringContent("{ ceci n'est pas du json }", Encoding.UTF8, "application/json");

        var reponse = await _factory.CreateAuthenticatedClient().PostAsync(PlansEndpoint, contenu);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_PIPE_02_RouteInexistante_Retourne404()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync("/api/v1/route-qui-nexiste-pas");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_PIPE_03_GuidMalforme_NeCorrespondPasALaRoute()
    {
        var contenu = JsonContent.Create(new { });

        var reponse = await _factory.CreateAuthenticatedClient().PutAsync($"{PlansEndpoint}/pas-un-guid", contenu);

        // La contrainte {id:guid} fait partie du filtrage de route : une valeur non conforme
        // empêche la route de correspondre, ce qui donne 404 et non 400. Le recensement annonce
        // 400 — écart signalé, le comportement réel d'ASP.NET Core fait foi.
        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }
}
