namespace NutritionApi.Api.Tests.Integration.Fixtures;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

/// <summary>
/// Schéma d'authentification des tests de niveau 2 : construit un <see cref="ClaimsPrincipal"/> à
/// partir d'en-têtes, sans Keycloak ni signature à vérifier.
/// </summary>
/// <remarks>
/// La validation réelle des jetons — signature, issuer, audience, expiration — n'est pas testable
/// ici : ce handler remplace précisément le composant qui s'en charge. Elle relève du niveau 3
/// (NTR-28), où Keycloak tourne.
/// <para>
/// L'identité passe par des en-têtes plutôt que par un état porté par la fabrique : xUnit exécute
/// les classes de test en parallèle, et un état partagé rendrait les résultats dépendants de
/// l'ordre d'exécution.
/// </para>
/// </remarks>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    /// <summary>Nom du schéma, substitué au JWT Bearer dans la fabrique de test.</summary>
    public const string SchemeName = "Test";

    /// <summary>En-tête portant l'identifiant Keycloak — l'équivalent du claim <c>sub</c>.</summary>
    public const string SubjectHeader = "X-Test-Sub";

    /// <summary>En-tête portant les rôles, séparés par des virgules.</summary>
    public const string RolesHeader = "X-Test-Roles";

    /// <summary>Construit le handler.</summary>
    /// <param name="options">Options du schéma.</param>
    /// <param name="logger">Fabrique de journaux.</param>
    /// <param name="encoder">Encodeur d'URL.</param>
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    /// <summary>
    /// Authentifie la requête si l'en-tête de sujet est présent. Son absence vaut requête anonyme —
    /// c'est ce qui permet de vérifier les 401 sans avoir à démonter le pipeline.
    /// </summary>
    /// <returns>Le résultat d'authentification.</returns>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubjectHeader, out var subject) || string.IsNullOrWhiteSpace(subject))
            return Task.FromResult(AuthenticateResult.NoResult());

        // Le claim garde son nom d'origine : l'API configure MapInboundClaims = false, et
        // UserResolutionMiddleware lit littéralement "sub".
        var claims = new List<Claim> { new("sub", subject.ToString()) };

        if (Request.Headers.TryGetValue(RolesHeader, out var roles))
        {
            claims.AddRange(roles.ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(role => new Claim(ClaimTypes.Role, role)));
        }

        // Le type de rôle est déclaré explicitement, sans quoi IsInRole et la policy AdminOnly
        // ne reconnaissent pas les claims posés ci-dessus.
        var identity = new ClaimsIdentity(claims, SchemeName, ClaimTypes.Name, ClaimTypes.Role);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
