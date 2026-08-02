namespace NutritionApi.Infrastructure.Observability;

using System.Diagnostics.Metrics;

/// <summary>
/// Compteurs propres à la couche Infrastructure (NTR-138) : cache de recherche et jobs planifiés.
/// </summary>
/// <remarks>
/// Ces deux mesures n'existent nulle part ailleurs. La durée des requêtes HTTP, celle des commandes
/// SQL et l'état du pool de connexions sont produits par les bibliothèques elles-mêmes ; un taux de
/// succès de cache et l'issue d'un job, non — ils naissent d'une décision prise dans ce code.
/// <para>
/// Le compteur est construit par <see cref="IMeterFactory"/> et non déclaré en statique : un
/// instrument statique est partagé par toute la durée du processus, et deux tests qui mesurent la
/// même chose se contamineraient.
/// </para>
/// </remarks>
public sealed class InfrastructureMetrics
{
    /// <summary>
    /// Nom du compteur. La couche API doit y abonner le fournisseur OpenTelemetry, faute de quoi
    /// les mesures sont produites sans que rien ne les recueille.
    /// </summary>
    public const string MeterName = "NutritionApi.Infrastructure";

    private readonly Counter<long> _recherchesPresenteesAuCache;
    private readonly Histogram<double> _dureeDesJobs;

    /// <summary>Construit les instruments.</summary>
    /// <param name="meterFactory">Fabrique fournie par l'hôte.</param>
    public InfrastructureMetrics(IMeterFactory meterFactory)
    {
        var compteur = meterFactory.Create(MeterName);

        _recherchesPresenteesAuCache = compteur.CreateCounter<long>(
            name: "nutrition.cache.lookups",
            unit: "{lookup}",
            description: "Recherches d'aliments présentées au cache, réparties par issue.");

        _dureeDesJobs = compteur.CreateHistogram<double>(
            name: "nutrition.job.duration",
            unit: "s",
            description: "Durée d'exécution des jobs planifiés, répartie par job et par issue.");
    }

    /// <summary>Le cache a répondu : la base n'a pas été interrogée.</summary>
    public void CacheHit() => CompterRecherche("hit");

    /// <summary>Le cache n'avait pas la réponse.</summary>
    public void CacheMiss() => CompterRecherche("miss");

    /// <summary>
    /// Le cache était injoignable. Distingué du défaut de cache à dessein : les deux renvoient
    /// l'appelant vers la base, mais un cache en panne se présenterait sinon comme un cache qui ne
    /// sert jamais — deux situations qui n'appellent pas la même intervention.
    /// </summary>
    public void CacheFailure() => CompterRecherche("failure");

    /// <summary>Enregistre la durée et l'issue d'un job qui vient de s'achever.</summary>
    /// <param name="job">Nom du job — la classe qui porte la méthode exécutée.</param>
    /// <param name="issue"><c>success</c> ou <c>failure</c>.</param>
    /// <param name="duree">Temps écoulé.</param>
    public void JobExecute(string job, string issue, TimeSpan duree) =>
        _dureeDesJobs.Record(
            duree.TotalSeconds,
            new KeyValuePair<string, object?>("job", job),
            new KeyValuePair<string, object?>("outcome", issue));

    /// <summary>
    /// Une seule série, étiquetée par issue : le taux de succès se calcule à la lecture, et
    /// n'a donc pas à être maintenu ici.
    /// </summary>
    private void CompterRecherche(string issue) =>
        _recherchesPresenteesAuCache.Add(1, new KeyValuePair<string, object?>("outcome", issue));
}
