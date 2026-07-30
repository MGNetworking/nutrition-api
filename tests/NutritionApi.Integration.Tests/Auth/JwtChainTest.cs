namespace NutritionApi.Integration.Tests.Auth;

using System.Net;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Integration.Tests.Fixtures;

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
    public async Task IT_EXT_09_Jeton_Keycloak_ouvre_l_acces_a_users_me()
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
    public async Task IT_EXT_10_Jeton_sans_role_admin_refuse_le_dashboard()
    {
        await EnsureUserAsync(KeycloakTokens.StandardUserSubject);

        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.StandardUser);

        var response = await client.GetAsync("/api/v1/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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
