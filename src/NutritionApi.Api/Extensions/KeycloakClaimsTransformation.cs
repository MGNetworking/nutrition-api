using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace NutritionApi.Api.Extensions
{
    // Keycloak stocke les rôles dans un format non standard que ASP.NET
    // Core ne comprend pas nativement. On utilise un IClaimsTransformation
    // pour convertir automatiquement ces rôles en claims standards à chaque
    // requête authentifiée.
    public class KeycloakClaimsTransformation : IClaimsTransformation
    {
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
