namespace NutritionApi.Api.Tests.Integration;

using NutritionApi.Api.Tests.Integration.Fixtures;
using System.Net;

/// <summary>
/// Vérifie le socle lui-même, avant tout test métier : l'application doit démarrer sans PostgreSQL,
/// Redis ni Keycloak, et le pipeline d'authentification doit répondre comme en production.
/// </summary>
public class ApiFactoryTest : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiFactoryTest(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Application_StartsWithoutExternalDependencies()
    {
        // Le seul fait d'obtenir un client prouve que l'hôte a démarré : sans RemoveAll<IHostedService>,
        // le serveur Hangfire tenterait de créer son schéma dans PostgreSQL et ferait échouer le démarrage.
        var client = _factory.CreateAnonymousClient();

        Assert.NotNull(client);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutIdentity_Returns401()
    {
        var response = await _factory.CreateAnonymousClient().GetAsync("/api/v1/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithoutAdminRole_Returns403()
    {
        var response = await _factory.CreateAuthenticatedClient().GetAsync("/api/v1/admin/dashboard");

        // L'identité est valide mais le rôle manque : la policy AdminOnly doit trancher.
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminRole_IsNotRejected()
    {
        var response = await _factory.CreateAuthenticatedClient(roles: "admin").GetAsync("/api/v1/admin/dashboard");

        // On ne juge pas le contenu — seulement que ni l'authentification ni l'autorisation ne bloquent.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatedRequest_WithUnknownUser_Returns401()
    {
        var response = await _factory
            .CreateAuthenticatedClient(subject: "99999999-9999-9999-9999-999999999999")
            .GetAsync("/api/v1/users/me");

        // Jeton valide mais aucun profil en base : c'est UserResolutionMiddleware qui refuse.
        // Ce test prouve que le middleware s'exécute et que la doublure d'IUserRepository est branchée.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
