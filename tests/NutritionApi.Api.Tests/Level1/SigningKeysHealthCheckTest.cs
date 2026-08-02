namespace NutritionApi.Api.Tests.Level1;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NutritionApi.Api.HealthChecks;

/// <summary>
/// Branches d'échec de la sonde des clés de signature.
/// </summary>
/// <remarks>
/// Le cas nominal et celui du serveur d'identité coupé sont éprouvés au niveau 3, contre un vrai
/// Keycloak. Restent trois chemins qu'aucune coupure de conteneur ne produit : une autorité mal
/// configurée, un realm qui répond sans publier de clé, et un délai dépassé. Les provoquer au
/// niveau 3 demanderait de casser Keycloak de trois façons différentes ; ici une doublure suffit.
/// </remarks>
[Trait("Level", "1")]
public class SigningKeysHealthCheckTest
{
    /// <summary>Délai court : la branche du renoncement se vérifie en millisecondes, pas en secondes.</summary>
    private static readonly TimeSpan DelaiCourt = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// autorité absente : la sonde échoue en nommant la clé de configuration.
    /// </summary>
    /// <remarks>
    /// Sans <c>Keycloak:Authority</c>, ASP.NET ne construit aucun gestionnaire de configuration OIDC.
    /// Le message doit désigner la clé : c'est une erreur de déploiement, pas une panne, et elle se
    /// corrige en une ligne d'environnement.
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_ShouldFailNamingTheKey_WhenNoConfigurationManager()
    {
        var sonde = CreateSonde(configurationManager: null);

        var resultat = await sonde.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, resultat.Status);
        Assert.Contains("Keycloak:Authority", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// realm joignable mais sans clé publiée : la sonde échoue.
    /// </summary>
    /// <remarks>
    /// Cas sournois : le serveur d'identité répond, donc tout paraît sain, mais aucun jeton ne peut
    /// être validé. Une sonde qui se contenterait de « le serveur a répondu » laisserait passer une
    /// instance incapable d'authentifier quiconque.
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_ShouldFail_WhenRealmPublishesNoSigningKey()
    {
        var sonde = CreateSonde(CreateManager(new OpenIdConnectConfiguration()));

        var resultat = await sonde.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, resultat.Status);
        Assert.Contains("aucune clé de signature", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// serveur d'identité qui ne répond pas : la sonde renonce au bout du délai.
    /// </summary>
    /// <remarks>
    /// Une sonde qui pend est pire qu'une sonde qui échoue : l'orchestrateur attendrait son propre
    /// délai d'expiration sans rien apprendre, et le pod resterait dans un état indéterminé.
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_ShouldFail_WhenIdentityServerNeverAnswers()
    {
        var manager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        manager
            .Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .Returns(async (CancellationToken jeton) =>
            {
                await Task.Delay(System.Threading.Timeout.Infinite, jeton);
                return new OpenIdConnectConfiguration();
            });

        var sonde = CreateSonde(manager.Object);

        var resultat = await sonde.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, resultat.Status);
        Assert.Contains("n'ont pas pu être obtenues", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>clés disponibles : la sonde est saine et annonce leur nombre.</summary>
    [Fact]
    public async Task CheckHealthAsync_ShouldSucceed_WhenSigningKeysAreAvailable()
    {
        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(new SymmetricSecurityKey(new byte[32]));

        var sonde = CreateSonde(CreateManager(configuration));

        var resultat = await sonde.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, resultat.Status);
        Assert.Contains("1 clé(s)", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>Construit un gestionnaire de configuration rendant toujours la même configuration.</summary>
    private static IConfigurationManager<OpenIdConnectConfiguration> CreateManager(
        OpenIdConnectConfiguration configuration)
    {
        var manager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();

        manager
            .Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        return manager.Object;
    }

    /// <summary>Construit la sonde autour d'options ne portant que le gestionnaire voulu.</summary>
    private static SigningKeysHealthCheck CreateSonde(
        IConfigurationManager<OpenIdConnectConfiguration>? configurationManager)
    {
        var options = new JwtBearerOptions
        {
            Authority = "http://localhost:8778/realms/nutrition",
            ConfigurationManager = configurationManager
        };

        var monitor = new Mock<IOptionsMonitor<JwtBearerOptions>>();
        monitor.Setup(m => m.Get(JwtBearerDefaults.AuthenticationScheme)).Returns(options);

        return new SigningKeysHealthCheck(monitor.Object, DelaiCourt);
    }
}
