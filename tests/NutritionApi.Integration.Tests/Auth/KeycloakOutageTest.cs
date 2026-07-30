namespace NutritionApi.Integration.Tests.Auth;

using System.Net;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Integration.Tests.Fixtures;

/// <summary>
/// IT-EXT-16 et IT-EXT-17 — comportement de l'API quand le serveur d'identité est indisponible.
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
    /// IT-EXT-16 — serveur d'identité arrêté, clés déjà en cache : les requêtes authentifiées
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
    public async Task IT_EXT_16_Serveur_d_identite_arrete_les_jetons_en_cours_restent_valides()
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
        }
    }

    /// <summary>
    /// IT-EXT-17 — serveur d'identité arrêté au démarrage : l'application refuse de démarrer.
    /// </summary>
    /// <remarks>
    /// Sans ce garde-fou, une instance démarrée pendant une indisponibilité n'a aucune clé en cache :
    /// elle accepte le trafic et refuse **tous** les jetons, pendant que ses voisines fonctionnent.
    /// Deux instances derrière le même service, deux comportements.
    /// <para>
    /// <c>KeycloakAvailabilityService</c> force la récupération des clés au démarrage et interrompt
    /// l'hôte si elle n'aboutit pas dans le délai imparti. Le test vérifie que la création d'un client
    /// — qui déclenche le démarrage de l'hôte — lève bien.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task IT_EXT_17_Serveur_d_identite_arrete_au_demarrage_l_hote_refuse_de_demarrer()
    {
        DockerContainer.Stop(DockerContainer.Keycloak);

        try
        {
            await using var aFroid = new IntegrationFactory();

            // CreateClient() construit l'hôte et exécute les services hébergés : c'est là que l'échec
            // se produit, pas à la construction de la fabrique.
            var echec = Assert.ThrowsAny<Exception>(() => aFroid.CreateClient());

            Assert.Contains(
                "clés de signature",
                DeroulerLesCauses(echec),
                StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Keycloak);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Keycloak, TimeSpan.FromSeconds(240));
        }
    }

    /// <summary>Concatène les messages d'une exception et de toutes ses causes.</summary>
    /// <param name="exception">Exception de tête.</param>
    /// <returns>Les messages, du plus externe au plus interne.</returns>
    /// <remarks>
    /// L'hôte enveloppe l'échec d'un service hébergé : le message d'origine n'est pas celui de
    /// l'exception de premier niveau.
    /// </remarks>
    private static string DeroulerLesCauses(Exception exception)
    {
        var messages = new List<string>();

        for (Exception? courante = exception; courante is not null; courante = courante.InnerException)
            messages.Add(courante.Message);

        return string.Join(" | ", messages);
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
