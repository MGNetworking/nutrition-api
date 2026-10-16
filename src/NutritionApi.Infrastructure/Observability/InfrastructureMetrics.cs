namespace NutritionApi.Infrastructure.Observability;

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

/// <summary>
/// Compteurs propres à la couche Infrastructure : cache de recherche et jobs planifiés (NTR-138),
/// puis état de la base de données (NTR-141).
/// </summary>
/// <remarks>
/// Ces mesures n'existent nulle part ailleurs. La durée des requêtes HTTP, celle des commandes
/// SQL et l'état du pool de connexions sont produits par les bibliothèques elles-mêmes ; un taux de
/// succès de cache, l'issue d'un job et la croissance des tables Hangfire, non — ils naissent d'une
/// décision prise dans ce code.
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
    private readonly Counter<long> _connexionsRejetees;
    private readonly ObservableGauge<long> _connexionsParEtat;
    private readonly ObservableGauge<long> _lignesParTableHangfire;

    // Dernières valeurs connues, lues par les jauges au moment de la collecte. Une jauge observable
    // publie un état, pas un cumul : il n'y a rien à additionner, seulement à refléter.
    private readonly ConcurrentDictionary<string, long> _etatsDesConnexions = new();
    private readonly ConcurrentDictionary<string, long> _taillesDesTablesHangfire = new();

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

        // État des connexions à PostgreSQL, relevé périodiquement (NTR-141). Une jauge et non un
        // compteur : « 12 connexions actives » est une photographie, pas un cumul depuis le
        // démarrage — un compteur ne redescendrait jamais.
        _connexionsParEtat = compteur.CreateObservableGauge(
            name: "nutrition.db.connections",
            observeValues: () => _etatsDesConnexions.Select(etat =>
                new Measurement<long>(etat.Value, new KeyValuePair<string, object?>("state", etat.Key))),
            unit: "{connection}",
            description: "Connexions PostgreSQL, réparties par état (active, waiting, idle).");

        // Tentatives d'ouverture refusées : la base injoignable, le pool épuisé. Compteur, car
        // chaque rejet est un événement — et c'est le seul des trois états que ce code peut
        // constater lui-même. Les connexions actives et en attente viennent de la relève.
        _connexionsRejetees = compteur.CreateCounter<long>(
            name: "nutrition.db.connections.rejected",
            unit: "{connection}",
            description: "Tentatives de connexion à PostgreSQL rejetées (base injoignable, pool épuisé).");

        // Croissance des tables du schéma hangfire (NTR-141). Elles grossissent sans purge : la
        // tendance est l'information utile, d'où une jauge relue à chaque collecte.
        _lignesParTableHangfire = compteur.CreateObservableGauge(
            name: "nutrition.hangfire.rows",
            observeValues: () => _taillesDesTablesHangfire.Select(table =>
                new Measurement<long>(table.Value, new KeyValuePair<string, object?>("table", table.Key))),
            unit: "{row}",
            description: "Nombre de lignes des tables du schéma hangfire, réparties par table.");
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

    /// <summary>Publie le nombre de connexions ouvertes pour un état donné.</summary>
    /// <param name="etat"><c>active</c>, <c>waiting</c> ou <c>idle</c>.</param>
    /// <param name="nombre">Nombre de connexions dans cet état.</param>
    public void Connexions(string etat, long nombre) => _etatsDesConnexions[etat] = nombre;

    /// <summary>Compte une tentative de connexion à PostgreSQL rejetée.</summary>
    public void ConnexionRejetee() => _connexionsRejetees.Add(1);

    /// <summary>Publie le nombre de lignes d'une table du schéma <c>hangfire</c>.</summary>
    /// <param name="table">Nom de la table — <c>job</c>, <c>state</c> ou <c>counter</c>.</param>
    /// <param name="lignes">Nombre de lignes.</param>
    public void TailleTableHangfire(string table, long lignes) => _taillesDesTablesHangfire[table] = lignes;

    /// <summary>
    /// Une seule série, étiquetée par issue : le taux de succès se calcule à la lecture, et
    /// n'a donc pas à être maintenu ici.
    /// </summary>
    private void CompterRecherche(string issue) =>
        _recherchesPresenteesAuCache.Add(1, new KeyValuePair<string, object?>("outcome", issue));
}
