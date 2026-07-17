using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace NutritionApi.Api.Extensions
{
    /// <summary>
    /// Transforme les claims Keycloak en claims standards ASP.NET Core.
    /// </summary>
    /// <remarks>
    /// Keycloak stocke les rôles dans le claim <c>realm_access.roles</c>, un format non standard
    /// qu'ASP.NET Core ne reconnaît pas nativement via <see cref="ClaimTypes.Role"/>.
    /// Cette transformation est exécutée automatiquement à chaque requête authentifiée
    /// et extrait les rôles pour les injecter comme claims <see cref="ClaimTypes.Role"/> standards,
    /// ce qui permet l'utilisation de <c>[Authorize(Roles = "...")]</c> et <c>User.IsInRole(...)</c>.
    /// </remarks>
    public class KeycloakClaimsTransformation : IClaimsTransformation
    {
        /// <summary>Extrait les rôles du claim <c>realm_access</c> Keycloak et les ajoute comme claims <see cref="ClaimTypes.Role"/>.</summary>
        /// <param name="principal">Le principal authentifié de la requête courante.</param>
        /// <returns>Le principal enrichi avec les claims de rôle standards.</returns>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            var realmAccess = principal.FindFirstValue("realm_access");
            if (realmAccess is null) return Task.FromResult(principal);

            var json = JsonDocument.Parse(realmAccess);
            if (!json.RootElement.TryGetProperty("roles", out var roles))
                return Task.FromResult(principal);

            var identity = new ClaimsIdentity();
            foreach (var role in roles.EnumerateArray())
            {
                var roleName = role.GetString();
                if (roleName is not null)
                    identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
            }

            principal.AddIdentity(identity);
            return Task.FromResult(principal);
        }
    }
}
