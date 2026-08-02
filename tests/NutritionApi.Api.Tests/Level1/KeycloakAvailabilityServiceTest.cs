namespace NutritionApi.Api.Tests.Level1;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using NutritionApi.Api.Startup;

/// <summary>
/// Le préchargement des clés du realm ne doit jamais interrompre le démarrage.
/// </summary>
/// <remarks>
/// Ce service interrompait l'hôte jusqu'au 2026-08-02 (NTR-173). Le raisonnement d'origine était
/// juste — une instance sans clés refuse tous les jetons — mais le remède coûtait plus que le mal :
/// un conteneur qui sort en erreur part en <c>CrashLoopBackOff</c> dont le délai double jusqu'à cinq
/// minutes, si bien qu'un serveur d'identité en retard de quatre minutes rendait l'API indisponible
/// bien plus longtemps que lui.
/// <para>
/// C'est désormais la sonde <c>/health/ready</c> qui tient l'instance hors du service. Ces cas
/// verrouillent le fait que le démarrage, lui, aboutit — sans quoi la boucle de redémarrage
/// reviendrait sans qu'on s'en aperçoive.
/// </para>
/// <para>
/// À ne pas confondre avec <see cref="KeycloakAdminConfigurationValidator"/>, qui interrompt
/// toujours le démarrage : une clé de configuration absente est une erreur de déploiement, elle ne
/// se répare pas d'elle-même. Une indisponibilité, si.
/// </para>
/// </remarks>
[Trait("Level", "1")]
public class KeycloakAvailabilityServiceTest
{
    /// <summary>
    /// autorité absente : le service journalise et rend la main, sans lever.
    /// </summary>
    /// <remarks>
    /// Sans <c>Keycloak:Authority</c>, aucun gestionnaire de configuration OIDC n'existe. C'est une
    /// erreur de déploiement — mais la faire remonter en interrompant l'hôte reproduirait la boucle
    /// de redémarrage. L'instance démarre, se déclare non prête, et le journal nomme la cause.
    /// </remarks>
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenNoConfigurationManager()
    {
        var journal = new Mock<ILogger<KeycloakAvailabilityService>>();

        var service = CreateService(configurationManager: null, journal);

        await service.StartAsync(CancellationToken.None);

        AssertJournaliseUneErreur(journal);
    }

    /// <summary>
    /// serveur d'identité injoignable : le service renonce au bout du délai, sans lever.
    /// </summary>
    /// <remarks>
    /// C'est le cas qui produisait l'arrêt du processus. Le délai est ramené à une seconde par la
    /// configuration : attendre les soixante secondes applicatives n'apprendrait rien de plus.
    /// </remarks>
    [Fact]
    public async Task StartAsync_ShouldNotThrow_WhenIdentityServerIsUnreachable()
    {
        var manager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        manager
            .Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("connexion refusée"));

        var journal = new Mock<ILogger<KeycloakAvailabilityService>>();

        var service = CreateService(manager.Object, journal, delaiSecondes: 1);

        await service.StartAsync(CancellationToken.None);

        AssertJournaliseUneErreur(journal);
    }

    /// <summary>clés disponibles : le préchargement aboutit sans rien journaliser en erreur.</summary>
    [Fact]
    public async Task StartAsync_ShouldSucceed_WhenSigningKeysAreAvailable()
    {
        var configuration = new OpenIdConnectConfiguration();
        configuration.SigningKeys.Add(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(new byte[32]));

        var manager = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        manager
            .Setup(m => m.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(configuration);

        var journal = new Mock<ILogger<KeycloakAvailabilityService>>();

        await CreateService(manager.Object, journal).StartAsync(CancellationToken.None);

        journal.Verify(
            j => j.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    /// <summary>Vérifie qu'une erreur a bien été journalisée — la seule trace laissée par un échec.</summary>
    private static void AssertJournaliseUneErreur(Mock<ILogger<KeycloakAvailabilityService>> journal)
        => journal.Verify(
            j => j.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);

    /// <summary>Construit le service autour du gestionnaire voulu.</summary>
    private static KeycloakAvailabilityService CreateService(
        IConfigurationManager<OpenIdConnectConfiguration>? configurationManager,
        Mock<ILogger<KeycloakAvailabilityService>> journal,
        int delaiSecondes = 1)
    {
        var options = new JwtBearerOptions
        {
            Authority = "http://localhost:8778/realms/nutrition",
            ConfigurationManager = configurationManager
        };

        var monitor = new Mock<IOptionsMonitor<JwtBearerOptions>>();
        monitor.Setup(m => m.Get(JwtBearerDefaults.AuthenticationScheme)).Returns(options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [KeycloakAvailabilityService.TimeoutSettingKey] = delaiSecondes.ToString()
            })
            .Build();

        return new KeycloakAvailabilityService(monitor.Object, configuration, journal.Object);
    }
}
