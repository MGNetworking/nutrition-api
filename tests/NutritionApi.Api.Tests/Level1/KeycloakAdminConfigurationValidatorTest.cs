namespace NutritionApi.Api.Tests.Level1;

using Microsoft.Extensions.Options;
using NutritionApi.Api.Startup;
using NutritionApi.Infrastructure.ExternalServices.Keycloak;

[Trait("Level", "1")]
public class KeycloakAdminConfigurationValidatorTest
{
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenConfigurationIsComplete()
    {
        var validator = CreateValidator(CreateCompleteOptions());

        await validator.StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_ShouldThrowNamingTheKey_WhenAdminBaseUrlIsMissing()
    {
        var options = CreateCompleteOptions();
        options.AdminBaseUrl = string.Empty;

        await AssertThrowsNamingAsync(options, "Keycloak:AdminBaseUrl");
    }

    [Fact]
    public async Task StartAsync_ShouldThrowNamingTheKey_WhenRealmIsMissing()
    {
        var options = CreateCompleteOptions();
        options.Realm = string.Empty;

        await AssertThrowsNamingAsync(options, "Keycloak:Realm");
    }

    [Fact]
    public async Task StartAsync_ShouldThrowNamingTheKey_WhenServiceClientIdIsMissing()
    {
        var options = CreateCompleteOptions();
        options.ServiceClientId = string.Empty;

        await AssertThrowsNamingAsync(options, "Keycloak:ServiceClientId");
    }

    [Fact]
    public async Task StartAsync_ShouldThrowNamingTheKey_WhenServiceClientSecretIsMissing()
    {
        var options = CreateCompleteOptions();
        options.ServiceClientSecret = string.Empty;

        await AssertThrowsNamingAsync(options, "Keycloak:ServiceClientSecret");
    }

    /// <summary>
    /// Une variable d'environnement définie mais vide arrive comme une suite d'espaces, pas comme
    /// une chaîne vide : le contrôle doit la traiter comme absente.
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldThrowNamingTheKey_WhenServiceClientSecretIsWhitespace()
    {
        var options = CreateCompleteOptions();
        options.ServiceClientSecret = "   ";

        await AssertThrowsNamingAsync(options, "Keycloak:ServiceClientSecret");
    }

    /// <summary>
    /// Le message doit nommer toutes les clés manquantes d'un coup — sinon un déploiement se corrige
    /// par redémarrages successifs, une clé à la fois.
    /// </summary>
    [Fact]
    public async Task StartAsync_ShouldThrowNamingAllKeys_WhenSeveralAreMissing()
    {
        var options = CreateCompleteOptions();
        options.Realm = string.Empty;
        options.ServiceClientSecret = string.Empty;

        await AssertThrowsNamingAsync(options, "Keycloak:Realm", "Keycloak:ServiceClientSecret");
    }

    private static async Task AssertThrowsNamingAsync(KeycloakAdminOptions options, params string[] expectedKeys)
    {
        var validator = CreateValidator(options);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => validator.StartAsync(CancellationToken.None));

        foreach (var key in expectedKeys)
            Assert.Contains(key, exception.Message, StringComparison.Ordinal);
    }

    private static KeycloakAdminConfigurationValidator CreateValidator(KeycloakAdminOptions options)
        => new(Options.Create(options));

    private static KeycloakAdminOptions CreateCompleteOptions() => new()
    {
        AdminBaseUrl        = "http://localhost:8778",
        Realm               = "nutrition",
        ServiceClientId     = "nutrition-api-service",
        ServiceClientSecret = "un-secret"
    };
}
