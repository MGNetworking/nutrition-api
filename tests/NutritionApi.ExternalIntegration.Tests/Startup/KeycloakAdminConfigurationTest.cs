namespace NutritionApi.ExternalIntegration.Tests.Startup;

using Microsoft.AspNetCore.Hosting;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// comportement de l'application au démarrage selon que les paramètres d'administration Keycloak
/// sont fournis ou non.
/// </summary>
/// <remarks>
/// Les tests unitaires de <c>KeycloakAdminConfigurationValidator</c> établissent qu'il lève quand une
/// clé est vide. Ils ne disent rien de ce que le ticket exige réellement : que **l'application**
/// refuse de démarrer. Entre les deux se trouvent l'enregistrement dans <c>Program.cs</c> et la
/// remontée de l'exception jusqu'à l'hôte — deux choses qu'aucune doublure ne peut prouver.
/// <para>
/// Aucun conteneur n'est arrêté ici : il suffit de vider une clé de configuration. Ce cas appartient
/// tout de même au niveau 3, parce que ce qu'il éprouve est le démarrage de l'application réelle.
/// </para>
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class KeycloakAdminConfigurationTest(IntegrationFactory factory)
{
    /// <summary>
    /// secret du client de service absent : l'application refuse de démarrer, en nommant la clé.
    /// </summary>
    /// <remarks>
    /// Sans ce garde-fou, l'API démarrerait normalement et la purge RGPD échouerait à chaque
    /// exécution nocturne — <c>TryPurgeAsync</c> intercepte toute exception, la journalise et rend
    /// <c>false</c>. Des comptes dont le délai de grâce est expiré resteraient dans le realm sans que
    /// rien ne le signale ailleurs que dans les journaux.
    /// </remarks>
    [Fact]
    public void Host_ShouldFailToStart_WhenServiceClientSecretIsMissing()
    {
        // Le délégué s'applique après ConfigureWebHost de la fabrique : c'est ce qui permet d'écraser
        // le secret qu'elle a lu dans l'export du realm.
        using var sansSecret = factory.WithWebHostBuilder(
            builder => builder.UseSetting("Keycloak:ServiceClientSecret", string.Empty));

        // CreateClient() construit l'hôte et exécute les services hébergés : c'est là que l'échec se
        // produit, pas à la construction de la fabrique.
        var echec = Assert.ThrowsAny<Exception>(() => sansSecret.CreateClient());

        Assert.Contains(
            "Keycloak:ServiceClientSecret",
            ExceptionChain.DeroulerLesCauses(echec),
            StringComparison.Ordinal);
    }

    /// <summary>configuration complète : le démarrage n'est pas entravé.</summary>
    /// <remarks>
    /// Contrepartie du cas précédent. Sans lui, un contrôle trop strict — qui refuserait aussi une
    /// configuration valide — laisserait le cas négatif vert : l'hôte lèverait, le message nommerait
    /// la clé, et rien ne distinguerait le garde-fou d'un blocage permanent.
    /// </remarks>
    [Fact]
    public void Host_ShouldStart_WhenConfigurationIsComplete()
    {
        using var client = factory.CreateClient();

        Assert.NotNull(client);
    }
}
