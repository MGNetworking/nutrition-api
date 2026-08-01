namespace NutritionApi.Api.Tests.Level1;

using System.Security.Claims;
using NutritionApi.Api.Extensions;

[Trait("Level", "1")]
public class KeycloakClaimsTransformationTest
{
    private readonly KeycloakClaimsTransformation _transformation = new();

    [Fact]
    public async Task TransformAsync_WithRoles_AddsRoleClaims()
    {
        var principal = CreatePrincipalWithRealmAccess("{\"roles\":[\"user\",\"admin\"]}");

        var result = await _transformation.TransformAsync(principal);

        Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Role && c.Value == "user");
        Assert.Contains(result.Claims, c => c.Type == ClaimTypes.Role && c.Value == "admin");
    }

    [Fact]
    public async Task TransformAsync_WithoutRealmAccess_ReturnsPrincipalUnchanged()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _transformation.TransformAsync(principal);

        Assert.DoesNotContain(result.Claims, c => c.Type == ClaimTypes.Role);
    }

    [Fact]
    public async Task TransformAsync_WithoutRolesProperty_ReturnsPrincipalUnchanged()
    {
        var principal = CreatePrincipalWithRealmAccess("{\"other\":[\"value\"]}");

        var result = await _transformation.TransformAsync(principal);

        Assert.DoesNotContain(result.Claims, c => c.Type == ClaimTypes.Role);
    }

    private static ClaimsPrincipal CreatePrincipalWithRealmAccess(string realmAccess)
    {
        var claims = new[] { new Claim("realm_access", realmAccess) };
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }
}
