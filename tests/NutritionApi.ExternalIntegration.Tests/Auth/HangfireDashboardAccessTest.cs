namespace NutritionApi.ExternalIntegration.Tests.Auth;

using System.Net;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// accès au dashboard Hangfire selon les rôles portés par le jeton.
/// </summary>
/// <remarks>
/// <c>/hangfire</c> est une interface web **ouverte par défaut** : elle affiche l'état des jobs, et
/// permet d'en déclencher comme d'en supprimer. Seul <c>HangfireAdminAuthorizationFilter</c> la
/// ferme, et rien n'éprouvait ce filtre — aucun test n'appelait cette route.
/// <para>
/// Le niveau 2 ne peut pas le faire : <c>TestAuthHandler</c> y remplace le schéma
/// d'authentification, et le dashboard disparaît du pipeline avec les services hébergés. Le filtre
/// s'appuie sur <c>IsInRole</c>, donc sur la conversion de <c>realm_access.roles</c> opérée par
/// <c>KeycloakClaimsTransformation</c> : il faut un jeton réellement émis par le realm.
/// </para>
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class HangfireDashboardAccessTest(IntegrationFactory factory)
{
    /// <summary>compte sans le rôle <c>admin</c> : le dashboard est refusé par un 403.</summary>
    /// <remarks>
    /// Le jeton est valide et l'API l'accepte partout ailleurs — c'est bien le rôle, et lui seul, qui
    /// distingue les deux cas de cette classe.
    /// <para>
    /// **403 et non 401**, et la nuance est le sujet du test. Hangfire choisit son statut selon
    /// l'identité : 401 quand elle est inconnue, 403 quand elle est connue mais non autorisée. Un 401
    /// signifierait donc que la requête n'a jamais atteint le filtre — c'était le cas avant la
    /// dispense de profil, <c>UserResolutionMiddleware</c> l'arrêtant plus tôt. Ce test passait alors
    /// sans rien prouver du filtre qu'il prétendait éprouver.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task GetHangfire_ShouldDenyAccess_WhenUserIsNotAdmin()
    {
        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.StandardUser);

        var reponse = await client.GetAsync("/hangfire");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    /// <summary>
    /// compte portant le rôle <c>admin</c>, sans profil applicatif : le dashboard répond.
    /// </summary>
    /// <remarks>
    /// Contrepartie indispensable : un filtre qui refuserait tout le monde laisserait le cas négatif
    /// vert.
    /// <para>
    /// **Aucune ligne <c>User</c> n'est semée, et c'est le cœur du cas.** L'administration et l'espace
    /// client sont disjoints : la table <c>users</c> porte un profil nutritionnel — date de naissance,
    /// taille, allergies — dont un administrateur n'a que faire. Le rôle <c>admin</c> du realm doit
    /// suffire à ouvrir le dashboard.
    /// </para>
    /// <para>
    /// Ce cas a d'abord échoué en 401. <c>/hangfire</c> ne quitte pas le pipeline avant
    /// <c>UserResolutionMiddleware</c> : l'endpoint est exécuté en fin de chaîne, après tous les
    /// middlewares déclarés, quel que soit l'endroit où <c>MapHangfireDashboard</c> apparaît dans
    /// <c>Program.cs</c>. La dispense est désormais explicite, portée par les métadonnées de
    /// l'endpoint.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task GetHangfire_ShouldGrantAccess_WhenUserIsAdminWithoutProfile()
    {
        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.AdminUser);

        var reponse = await client.GetAsync("/hangfire");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }
}
