namespace NutritionApi.ExternalIntegration.Tests.Observability;

using NutritionApi.ExternalIntegration.Tests.Fixtures;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

/// <summary>
/// Éprouve la trace sur une requête réelle (NTR-139) : l'arbre est-il relié, et l'identifiant
/// publié au client mène-t-il bien à cette trace ?
/// </summary>
/// <remarks>
/// Ce niveau est le seul où la question se pose. Aux niveaux 1 et 2, PostgreSQL et Redis sont
/// doublés : aucune commande réelle n'est émise, l'arbre serait vide et le test ne prouverait rien.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class TraceCorrelationTest(IntegrationFactory factory)
{
    /// <summary>Source des activités créées par ASP.NET Core pour chaque requête entrante.</summary>
    private const string SourceAspNetCore = "Microsoft.AspNetCore";

    /// <summary>Source des activités créées par le pilote PostgreSQL, une par commande.</summary>
    private const string SourceNpgsql = "Npgsql";

    /// <summary>
    /// Écoute toutes les activités le temps d'une requête. Un écouteur est indispensable : sans
    /// personne pour les recueillir, les bibliothèques n'en créent aucune — c'est ce qui rend
    /// l'instrumentation gratuite quand elle n'est pas exploitée.
    /// </summary>
    private static (ActivityListener Ecouteur, List<Activity> Recueillies) Ecouter()
    {
        var recueillies = new List<Activity>();

        var ecouteur = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activite =>
            {
                lock (recueillies)
                {
                    recueillies.Add(activite);
                }
            }
        };

        ActivitySource.AddActivityListener(ecouteur);

        return (ecouteur, recueillies);
    }

    /// <summary>Retourne les activités d'une source donnée, sous verrou.</summary>
    private static List<Activity> ParSource(List<Activity> recueillies, string source)
    {
        lock (recueillies)
        {
            return recueillies.Where(a => a.Source.Name.StartsWith(source, StringComparison.Ordinal)).ToList();
        }
    }

    [Fact]
    public async Task Request_ProducesASingleTreeSharingOneTraceId()
    {
        var client = await factory.CreateTokenClientAsync();
        var (ecouteur, recueillies) = Ecouter();

        using (ecouteur)
        {
            var reponse = await client.GetAsync("/api/v1/food-items?search=poulet");

            Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        }

        var requete = Assert.Single(ParSource(recueillies, SourceAspNetCore).Where(a => a.Parent is null));
        var commandes = ParSource(recueillies, SourceNpgsql);

        // Sans cette étape, la seule information disponible sur une requête lente resterait sa durée
        // totale — le §3 de la fiche d'observabilité en fait le problème à résoudre.
        Assert.NotEmpty(commandes);

        // Un arbre, non des mesures éparses : c'est l'identifiant partagé qui permet de dire lequel
        // des appels a consommé le temps.
        Assert.All(commandes, commande => Assert.Equal(requete.TraceId, commande.TraceId));
    }

    [Fact]
    public async Task ErrorResponse_PublishesTheTraceIdOfItsOwnTrace()
    {
        var client = await factory.CreateTokenClientAsync();
        var (ecouteur, recueillies) = Ecouter();

        HttpResponseMessage reponse;

        using (ecouteur)
        {
            reponse = await client.GetAsync($"/api/v1/diets/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        }

        var requete = Assert.Single(ParSource(recueillies, SourceAspNetCore).Where(a => a.Parent is null));

        var publie = JsonDocument
            .Parse(await reponse.Content.ReadAsStringAsync())
            .RootElement.GetProperty("traceId").GetString();

        // Le point de jonction du volet 3 : un utilisateur signale cet identifiant, et il mène à la
        // trace complète. Avant NTR-139, c'était l'identifiant de connexion Kestrel qui était publié
        // — il ne figure dans aucune trace et ne menait donc nulle part.
        Assert.Equal(requete.TraceId.ToString(), publie);
    }
}
