namespace NutritionApi.ExternalIntegration.Tests.Auth;

using System.Net;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// IT-EXT-09 et IT-EXT-10 — la chaîne d'authentification complète, avec un Keycloak réel.
/// </summary>
/// <remarks>
/// Transférés depuis NTR-74. Au niveau 2, <c>TestAuthHandler</c> remplace le schéma
/// d'authentification : il court-circuite précisément ce qui est éprouvé ici — l'issuer, l'audience
/// et la signature d'un jeton réellement émis.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class JwtChainTest(IntegrationFactory factory)
{
    /// <summary>
    /// IT-EXT-09 — un jeton émis par Keycloak ouvre l'accès à <c>GET /api/v1/users/me</c>.
    /// </summary>
    /// <remarks>
    /// Ce que le test prouve : l'API récupère les clés de signature du realm, valide l'issuer et
    /// trouve l'audience <c>nutrition-api</c> que le mapper du client ajoute au jeton. Un défaut sur
    /// l'un des trois donnerait 401.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_09_JetonEmisParKeycloak_Retourne200()
    {
        await EnsureUserAsync(KeycloakTokens.StandardUserSubject);

        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.StandardUser);

        var response = await client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    /// <summary>
    /// IT-EXT-10 — un jeton sans le rôle <c>admin</c> se voit refuser le tableau de bord.
    /// </summary>
    /// <remarks>
    /// Ce que le test prouve : les rôles voyagent bien dans <c>realm_access.roles</c> et la
    /// transformation de claims les convertit en rôles ASP.NET, sans quoi la policy AdminOnly
    /// laisserait passer. Le 403 — et non 401 — atteste que le jeton est valide mais insuffisant.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_10_JetonSansRoleAdmin_Retourne403()
    {
        await EnsureUserAsync(KeycloakTokens.StandardUserSubject);

        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.StandardUser);

        var response = await client.GetAsync("/api/v1/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// IT-EXT-18 — un jeton réellement expiré est refusé.
    /// </summary>
    /// <remarks>
    /// Ce cas figurait au recensement sous IT-AUTH-02, au niveau 2, où il ne pouvait pas être écrit :
    /// <c>TestAuthHandler</c> y remplace le composant qui vérifie l'expiration. Il n'avait jamais été
    /// repris ici.
    /// <para>
    /// Le jeton est <b>authentique</b> — émis par Keycloak, correctement signé, portant la bonne
    /// audience. Seule sa date d'expiration est dépassée. Un jeton forgé échouerait sur la signature
    /// et ne prouverait rien de <c>ValidateLifetime</c>.
    /// </para>
    /// <para>
    /// Le realm impose 1800 secondes de durée de vie ; le client <c>nutrition-api-tests-shortlived</c>
    /// la ramène à une seconde par son attribut <c>access.token.lifespan</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task IT_EXT_18_JetonExpire_Retourne401()
    {
        await EnsureUserAsync(KeycloakTokens.StandardUserSubject);

        var jeton = await factory.Tokens.GetAccessTokenAsync(
            KeycloakTokens.StandardUser, KeycloakTokens.ShortLivedClient);

        // La tolérance d'horloge par défaut de la validation JWT est de 5 minutes : elle est ramenée
        // à zéro dans la fabrique, sans quoi aucun jeton ne pourrait expirer à l'échelle d'un test.
        await Task.Delay(TimeSpan.FromSeconds(3));

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jeton);

        var response = await client.GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// IT-EXT-19 — un jeton dont l'audience ne désigne pas cette API est refusé.
    /// </summary>
    /// <remarks>
    /// Ce que le test protège : sans contrôle de l'audience, un jeton légitimement obtenu pour une
    /// autre API serait accepté par celle-ci. C'est le problème du <i>confused deputy</i> — le jeton
    /// est valide et signé, mais il ne nous était pas destiné.
    /// <para>
    /// Le client <c>nutrition-api-tests-no-audience</c> ne déclare aucun mapper d'audience : ses
    /// jetons ne portent pas <c>nutrition-api</c> dans leur claim <c>aud</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task IT_EXT_19_JetonSansLAudienceAttendue_Retourne401()
    {
        await EnsureUserAsync(KeycloakTokens.StandardUserSubject);

        var jeton = await factory.Tokens.GetAccessTokenAsync(
            KeycloakTokens.StandardUser, KeycloakTokens.NoAudienceClient);

        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", jeton);

        var response = await client.GetAsync("/api/v1/users/me");

        // 401 et non 403 : l'authentification elle-même échoue, la requête n'atteint jamais
        // l'autorisation ni la résolution de l'utilisateur.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Garantit la présence de la ligne <c>User</c> correspondant au sujet du jeton.
    /// </summary>
    /// <param name="keycloakId">Identifiant Keycloak porté par le jeton.</param>
    /// <remarks>
    /// <c>UserResolutionMiddleware</c> renvoie 401 quand le compte est absent de la base. Sans ce
    /// semis, IT-EXT-09 échouerait sur un 401 qui ne dirait rien de la validation du jeton.
    /// </remarks>
    private async Task EnsureUserAsync(string keycloakId)
    {
        await using var context = factory.NewContext();

        if (context.Users.Any(u => u.KeycloakId == keycloakId))
            return;

        context.Users.Add(new User(
            keycloakId: keycloakId,
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            height: 180,
            allergies: [],
            dietaryPreferences: []));

        await context.SaveChangesAsync();
    }
}
