namespace NutritionApi.ExternalIntegration.Tests.Startup;

using System.Net;
using System.Text.Json;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// Contrat des deux sondes de santé, éprouvé contre les vraies dépendances.
/// </summary>
/// <remarks>
/// Les deux points de terminaison répondent à des questions distinctes, et c'est cette distinction
/// que les cas ci-dessous verrouillent :
/// <list type="bullet">
///   <item><c>/health</c> — suis-je vivant ? Ne consulte rien, répond 200 tant que le processus
///   répond. C'est cette sonde que l'orchestrateur branche sur la vivacité : si elle échouait quand
///   une dépendance manque, le conteneur serait tué au lieu d'attendre.</item>
///   <item><c>/health/ready</c> — suis-je en état de servir ? PostgreSQL et les clés de signature
///   sont bloquants, Redis est signalé sans faire échouer le verdict.</item>
/// </list>
/// <para>
/// Le cas du démarrage à froid sans serveur d'identité est couvert par
/// <see cref="Auth.KeycloakOutageTest"/>, qui dispose déjà de la fabrique éphémère nécessaire.
/// </para>
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class HealthProbeTest(IntegrationFactory factory)
{
    /// <summary>pile complète : les deux sondes répondent 200.</summary>
    [Fact]
    public async Task Probes_ShouldReturn200_WhenAllDependenciesAreUp()
    {
        using var client = factory.CreateClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

        var aptitude = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, aptitude.StatusCode);
        Assert.Equal("Healthy", await LireStatutAsync(aptitude));
    }

    /// <summary>les sondes sont accessibles sans jeton.</summary>
    /// <remarks>
    /// Le kubelet n'en présente aucun. Ce cas verrouille aussi le fait que
    /// <c>UserResolutionMiddleware</c> les laisse passer : il n'agit que sur les requêtes déjà
    /// authentifiées, et un 401 ici rendrait les sondes inutilisables.
    /// </remarks>
    [Fact]
    public async Task Probes_ShouldBeAnonymous()
    {
        using var client = factory.CreateClient();

        Assert.NotEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/health")).StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, (await client.GetAsync("/health/ready")).StatusCode);
    }

    /// <summary>
    /// serveur d'identité arrêté alors que les clés sont en cache : l'instance reste prête.
    /// </summary>
    /// <remarks>
    /// C'est le cas qui a fait corriger la spécification de la sonde (NTR-172). Le prédicat retenu est
    /// « ai-je des clés utilisables ? », et non « le serveur d'identité répond-il ? ».
    /// <para>
    /// Avec le second, une coupure de Keycloak aurait rendu **toutes** les instances non prêtes — y
    /// compris celles qui validaient les jetons sans difficulté depuis leur cache. L'orchestrateur les
    /// aurait toutes retirées du service : une panne totale, causée par la sonde censée l'éviter.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Ready_ShouldStayHealthy_WhenIdentityServerIsDownAndKeysAreCached()
    {
        using var client = factory.CreateClient();

        // Peuple le cache des clés : sans cet appel préalable, le test prouverait l'inverse.
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);

        DockerContainer.Stop(DockerContainer.Keycloak);

        try
        {
            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

            var aptitude = await client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.OK, aptitude.StatusCode);
            Assert.Equal("Healthy", await LireStatutAsync(aptitude));
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Keycloak);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Keycloak, TimeSpan.FromSeconds(240));
        }
    }

    /// <summary>cache arrêté : l'instance est dégradée, et reste prête.</summary>
    /// <remarks>
    /// Redis est un accélérateur, pas une dépendance fonctionnelle : la recherche d'aliments retombe
    /// sur PostgreSQL. Retirer l'instance du service transformerait une dégradation en panne.
    /// <para>
    /// Le statut dégradé doit tout de même apparaître dans la réponse — c'est ce qui manquait quand
    /// Redis pouvait être mort depuis trois jours sans que rien ne le signale (NTR-150).
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Ready_ShouldReportDegradedButStay200_WhenCacheIsDown()
    {
        using var client = factory.CreateClient();

        DockerContainer.Stop(DockerContainer.Redis);

        try
        {
            var aptitude = await client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.OK, aptitude.StatusCode);

            var corps = await aptitude.Content.ReadAsStringAsync();

            Assert.Equal("Degraded", LireStatut(corps));
            Assert.Contains("redis", corps);
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Redis);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Redis, TimeSpan.FromSeconds(60));
        }
    }

    /// <summary>Lit le statut global du rapport de santé.</summary>
    /// <param name="reponse">Réponse HTTP d'une des deux sondes.</param>
    /// <returns>La valeur du champ <c>statut</c>.</returns>
    private static async Task<string?> LireStatutAsync(HttpResponseMessage reponse)
        => LireStatut(await reponse.Content.ReadAsStringAsync());

    /// <summary>Lit le statut global dans le corps JSON.</summary>
    /// <param name="corps">Corps de la réponse.</param>
    /// <returns>La valeur du champ <c>statut</c>.</returns>
    private static string? LireStatut(string corps)
        => JsonDocument.Parse(corps).RootElement.GetProperty("statut").GetString();
}
