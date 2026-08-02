namespace NutritionApi.ExternalIntegration.Tests.Auth;

using System.Net;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// comportement de l'API quand le serveur d'identité est indisponible.
/// </summary>
/// <remarks>
/// La validation des jetons est **locale** : l'API vérifie les signatures avec les clés publiques du
/// realm, récupérées une fois puis mises en cache. Ces deux cas éprouvent les deux faces de cette
/// propriété — elle protège des coupures après démarrage, elle rend le démarrage à froid dangereux.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class KeycloakOutageTest(IntegrationFactory factory)
{
    /// <summary>
    /// serveur d'identité arrêté, clés déjà en cache : les requêtes authentifiées
    /// continuent d'aboutir.
    /// </summary>
    /// <remarks>
    /// Ce que le test établit : une coupure du serveur d'identité est **invisible** pour les
    /// utilisateurs déjà porteurs d'un jeton valide. Aucun appel réseau n'a lieu par requête, la
    /// signature est vérifiée localement.
    /// <para>
    /// Ce qui casse en revanche, et que ce test ne couvre pas : l'émission de nouveaux jetons. Passé
    /// la durée de vie des jetons en circulation, plus personne ne peut entrer.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task GetUsersMe_ShouldReturn200_WhenIdentityServerIsDownAndKeysAreCached()
    {
        await EnsureUserAsync(KeycloakTokens.StandardUserSubject);

        // Le jeton est obtenu pendant que le serveur répond — c'est aussi ce qui garantit que les
        // clés du realm sont en cache au moment de la coupure.
        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.StandardUser);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/users/me")).StatusCode);

        DockerContainer.Stop(DockerContainer.Keycloak);

        try
        {
            var pendantLaCoupure = await client.GetAsync("/api/v1/users/me");

            Assert.Equal(HttpStatusCode.OK, pendantLaCoupure.StatusCode);
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Keycloak);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Keycloak, TimeSpan.FromSeconds(240));

            // Le conteneur sain ne suffit pas : l'hôte partagé peut être resté sans clés pendant la
            // coupure, et le test suivant récolterait un 401 sans rapport avec ce qu'il vérifie.
            await factory.WaitUntilReadyAsync();
        }
    }

    /// <summary>
    /// serveur d'identité arrêté au démarrage : l'application démarre, vivante mais non prête.
    /// </summary>
    /// <remarks>
    /// Le danger reste celui d'origine : une instance démarrée pendant une indisponibilité n'a aucune
    /// clé en cache, elle refuserait **tous** les jetons pendant que ses voisines fonctionnent.
    /// <para>
    /// Ce n'est plus l'arrêt du processus qui l'empêche, mais la sonde d'aptitude (NTR-173). Un
    /// conteneur qui sort en erreur est relancé par l'orchestrateur, et les échecs répétés mènent à un
    /// <c>CrashLoopBackOff</c> dont le délai double jusqu'à cinq minutes : un serveur d'identité en
    /// retard de quatre minutes rendait l'API indisponible bien plus longtemps que lui. Répondre 503
    /// sur <c>/health/ready</c> obtient le même résultat — aucun trafic — sans redémarrage, et
    /// l'instance rejoint le service d'elle-même dès qu'elle obtient ses clés.
    /// </para>
    /// <para>
    /// Ce que ce cas verrouille, et qui ne va pas de soi : <c>/health</c> répond <b>200</b> pendant ce
    /// temps. Si la sonde de vivacité échouait elle aussi, le conteneur serait tué et l'on retrouverait
    /// la boucle de redémarrage que l'on vient de supprimer.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Host_ShouldStartNotReady_WhenIdentityServerIsDownAtStartup()
    {
        DockerContainer.Stop(DockerContainer.Keycloak);

        try
        {
            // Le préchargement des clés réessaie jusqu'au bout de son délai avant de renoncer :
            // attendre les 60 s applicatives n'apprendrait rien de plus que deux secondes.
            await using var aFroid = IntegrationFactory.AvecPrechargementCourt(2);

            // CreateClient() construit l'hôte et exécute les services hébergés. C'est ici que le
            // démarrage échouait avant NTR-173.
            using var client = aFroid.CreateClient();

            Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);

            var aptitude = await client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, aptitude.StatusCode);

            // La réponse nomme la brique en cause : sans ce détail, un 503 ne distinguerait pas une
            // base absente d'un serveur d'identité absent.
            Assert.Contains("cles-de-signature", await aptitude.Content.ReadAsStringAsync());
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Keycloak);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Keycloak, TimeSpan.FromSeconds(240));

            // Le conteneur sain ne suffit pas : l'hôte partagé peut être resté sans clés pendant la
            // coupure, et le test suivant récolterait un 401 sans rapport avec ce qu'il vérifie.
            await factory.WaitUntilReadyAsync();
        }
    }

    /// <summary>Garantit la présence de la ligne <c>User</c> correspondant au sujet du jeton.</summary>
    /// <param name="keycloakId">Identifiant Keycloak porté par le jeton.</param>
    private async Task EnsureUserAsync(string keycloakId)
    {
        await using var context = factory.NewContext();

        if (context.Users.Any(u => u.KeycloakId == keycloakId))
            return;

        context.Users.Add(new User(
            keycloakId: keycloakId,
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            height: 180,
            allergies: [],
            dietaryPreferences: []));

        await context.SaveChangesAsync();
    }
}
