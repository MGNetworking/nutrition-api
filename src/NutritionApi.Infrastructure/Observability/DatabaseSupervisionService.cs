namespace NutritionApi.Infrastructure.Observability;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

/// <summary>Relève de l'état de la base : connexions ouvertes et volume des tables Hangfire.</summary>
/// <param name="ConnexionsActives">Sessions en train d'exécuter une requête, hors connexion de la relève.</param>
/// <param name="ConnexionsBloquees">Sessions actives bloquées sur un verrou ou une ressource.</param>
/// <param name="ConnexionsInactives">Sessions ouvertes mais au repos.</param>
/// <param name="TablesHangfire">
/// Nombre de lignes estimé par table du schéma <c>hangfire</c>, jamais négatif.
/// </param>
/// <param name="TaillesDisqueHangfire">
/// Taille sur disque de chaque table, en octets, index et TOAST compris. Mêmes clés que
/// <paramref name="TablesHangfire"/>.
/// </param>
public sealed record EtatBaseDonnees(
    long ConnexionsActives,
    long ConnexionsBloquees,
    long ConnexionsInactives,
    IReadOnlyDictionary<string, long> TablesHangfire,
    IReadOnlyDictionary<string, long> TaillesDisqueHangfire);

/// <summary>
/// Rend visible ce que fait PostgreSQL (NTR-141) : suivi des connexions et croissance des tables du
/// schéma <c>hangfire</c>.
/// </summary>
/// <remarks>
/// <para>
/// La relève est périodique et porte deux choses que rien d'autre ne surveille :
/// </para>
/// <list type="bullet">
/// <item>
/// l'état des connexions — actives, bloquées, inactives — tel que le serveur le voit, lu dans
/// <c>pg_stat_activity</c>. Complémentaire des mesures du pool publiées par Npgsql (NTR-138) :
/// celles-ci décrivent les requêtes du processus qui attendent une connexion libre, celles-là les
/// sessions reçues par le serveur. « Bloquée » est donc une session du serveur arrêtée sur un verrou
/// ou une ressource, jamais l'attente d'une connexion du pool. La connexion de la relève elle-même
/// est exclue des trois états.
/// </item>
/// <item>
/// le volume des tables <c>job</c>, <c>state</c> et <c>counter</c>. Hangfire n'embarque aucune purge
/// configurée : ces tables grossissent indéfiniment, et un volume anormal se voit d'autant mieux
/// qu'il est publié comme une tendance — c'est le rôle des jauges <c>nutrition.hangfire.rows</c> et
/// <c>nutrition.hangfire.bytes</c>.
/// </item>
/// </list>
/// <para>
/// Une relève décrit chaque table par ses lignes estimées <em>et</em> sa taille disque : c'est
/// <see cref="CollecterAsync"/>, la seule forme produite contre PostgreSQL.
/// </para>
/// <para>
/// Une relève qui échoue — base momentanément injoignable, schéma <c>hangfire</c> pas encore créé —
/// est journalisée puis retentée au cycle suivant : la supervision ne doit jamais faire tomber
/// l'application qu'elle observe.
/// </para>
/// </remarks>
public sealed class DatabaseSupervisionService : BackgroundService
{
    /// <summary>Section de configuration portant les réglages de la surveillance de la base.</summary>
    public const string SectionConfiguration = "DatabaseObservability";

    /// <summary>Intervalle entre deux relèves, en secondes, à défaut de configuration.</summary>
    public const int PeriodeParDefautSecondes = 300;

    /// <summary>
    /// Taille sur disque, en octets, à partir de laquelle une table Hangfire déclenche un
    /// avertissement : 512 Mo. Sous ce seuil, Hangfire a purgé normalement.
    /// </summary>
    public const long SeuilAlerteParDefautOctets = 536_870_912;

    private readonly ILogger<DatabaseSupervisionService> _logger;
    private readonly InfrastructureMetrics _metriques;
    private readonly string _connectionString;
    private readonly TimeSpan _periode;
    private readonly long _seuilAlerteOctets;

    /// <summary>Construit le service de supervision.</summary>
    /// <param name="logger">Journaliseur.</param>
    /// <param name="metriques">Instruments sur lesquels publier la relève.</param>
    /// <param name="configuration">Configuration de l'application.</param>
    /// <exception cref="InvalidOperationException">La chaîne de connexion <c>DefaultConnection</c> est absente.</exception>
    public DatabaseSupervisionService(
        ILogger<DatabaseSupervisionService> logger,
        InfrastructureMetrics metriques,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionConfiguration);

        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La chaîne de connexion 'DefaultConnection' est absente.");

        _periode = TimeSpan.FromSeconds(section.GetValue("SupervisionIntervalSeconds", PeriodeParDefautSecondes));
        _seuilAlerteOctets = section.GetValue("HangfireSizeWarningThresholdBytes", SeuilAlerteParDefautOctets);
        _logger = logger;
        _metriques = metriques;
    }

    /// <inheritdoc />
    /// <remarks>
    /// La première relève attend un intervalle : au démarrage, le schéma <c>hangfire</c> n'est pas
    /// encore créé et une base en cours de migration n'est pas interrogeable. Attendre évite un
    /// avertissement à chaque lancement, sans rien perdre — le processus vit bien plus longtemps.
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_periode, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            try
            {
                Rapporter(await CollecterAsync(stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Supervision de la base impossible — nouvelle relève dans {Periode} min.",
                    _periode.TotalMinutes);
            }
        }
    }

    /// <summary>Interroge PostgreSQL et retourne l'état courant.</summary>
    /// <param name="cancellationToken">Jeton d'annulation.</param>
    /// <returns>La relève : connexions par état et volume des tables Hangfire.</returns>
    /// <exception cref="NpgsqlException">La base est injoignable.</exception>
    public async Task<EtatBaseDonnees> CollecterAsync(CancellationToken cancellationToken = default)
    {
        await using var connexion = new NpgsqlConnection(_connectionString);
        await connexion.OpenAsync(cancellationToken);

        var connexions = await LireConnexionsAsync(connexion, cancellationToken);
        var tables = await LireTablesHangfireAsync(connexion, cancellationToken);

        return new EtatBaseDonnees(
            connexions.Actives,
            connexions.Bloquees,
            connexions.Inactives,
            tables.Lignes,
            tables.Octets);
    }

    /// <summary>Publie une relève : une mesure par état, et une entrée de journal.</summary>
    /// <param name="releve">La relève à restituer.</param>
    /// <remarks>
    /// Séparé de <see cref="CollecterAsync"/> à dessein : la restitution se vérifie sans base réelle,
    /// seule la collecte exige PostgreSQL.
    /// </remarks>
    public void Rapporter(EtatBaseDonnees releve)
    {
        _metriques.Connexions("active", releve.ConnexionsActives);
        _metriques.Connexions("blocked", releve.ConnexionsBloquees);
        _metriques.Connexions("idle", releve.ConnexionsInactives);

        _logger.LogInformation(
            "Connexions PostgreSQL — actives : {Actives}, bloquées : {Bloquees}, inactives : {Inactives}.",
            releve.ConnexionsActives,
            releve.ConnexionsBloquees,
            releve.ConnexionsInactives);

        foreach (var (table, lignes) in releve.TablesHangfire)
        {
            _metriques.LignesTableHangfire(table, lignes);

            var octets = releve.TaillesDisqueHangfire.TryGetValue(table, out var taille) ? taille : 0;

            _metriques.OctetsTableHangfire(table, octets);

            _logger.LogInformation(
                "Table hangfire.{Table} : {Lignes} lignes estimées, {Octets} octets sur disque.",
                table,
                lignes,
                octets);

            // Le seuil porte sur la taille disque seule : compter les lignes d'une table qui grossit
            // coûterait d'autant plus cher qu'elle est grosse, et une table volumineuse mais compacte
            // est normale. Le nombre de lignes reste publié comme mesure, sans avertissement.
            if (octets >= _seuilAlerteOctets)
            {
                _logger.LogWarning(
                    "Table hangfire.{Table} volumineuse : {Octets} octets sur disque (seuil {Seuil}) — purge à prévoir.",
                    table,
                    octets,
                    _seuilAlerteOctets);
            }
        }
    }

    /// <summary>
    /// Compte les sessions reçues par le serveur, par état. « Bloquées » désigne une session active
    /// arrêtée sur une ressource — un verrou, une entrée-sortie —, à distinguer d'une session au
    /// repos. La connexion de la relève ne se compte pas elle-même.
    /// </summary>
    private static async Task<(long Actives, long Bloquees, long Inactives)> LireConnexionsAsync(
        NpgsqlConnection connexion,
        CancellationToken cancellationToken)
    {
        // « pid <> pg_backend_pid() » : sans cette exclusion, la connexion qui exécute la requête
        // apparaîtrait active, et la relève se compterait elle-même dans chaque état où elle se
        // trouve. Toutes les autres sessions de la base sont, elles, comptées.
        const string sql = """
            select
                count(*) filter (where state = 'active' and wait_event_type is null)     as actives,
                count(*) filter (where state = 'active' and wait_event_type is not null) as bloquees,
                count(*) filter (where state = 'idle')                                   as inactives
            from pg_stat_activity
            where datname = current_database()
              and pid <> pg_backend_pid();
            """;

        await using var commande = new NpgsqlCommand(sql, connexion);
        await using var lecteur = await commande.ExecuteReaderAsync(cancellationToken);

        if (!await lecteur.ReadAsync(cancellationToken))
            return (0, 0, 0);

        return (lecteur.GetInt64(0), lecteur.GetInt64(1), lecteur.GetInt64(2));
    }

    /// <summary>
    /// Mesure le volume des trois tables que Hangfire ne purge pas de lui-même. Une seule requête
    /// catalogue suffit : PostgreSQL tient l'estimation du nombre de lignes et calcule la taille sur
    /// disque, là où un <c>count(*)</c> lirait toute la table — d'autant plus coûteux qu'elle grossit.
    /// Le schéma <c>hangfire</c> absent renvoie zéro ligne, sans erreur.
    /// </summary>
    private static async Task<(IReadOnlyDictionary<string, long> Lignes, IReadOnlyDictionary<string, long> Octets)>
        LireTablesHangfireAsync(NpgsqlConnection connexion, CancellationToken cancellationToken)
    {
        // n_live_tup est l'estimation suivie par PostgreSQL au fil des insertions ; elle n'est jamais
        // négative, et rien ne justifie de la recalculer. pg_total_relation_size donne la taille sur
        // disque, index et TOAST compris — c'est elle que le seuil d'avertissement observe.
        const string sql = """
            select
                c.relname::text                       as table,
                s.n_live_tup                          as lignes,
                pg_total_relation_size(c.oid)         as octets
            from pg_class c
            join pg_namespace n on n.oid = c.relnamespace
            join pg_stat_user_tables s on s.relid = c.oid
            where n.nspname = 'hangfire'
              and c.relname in ('job', 'state', 'counter');
            """;

        var lignes = new Dictionary<string, long>();
        var octets = new Dictionary<string, long>();

        await using var commande = new NpgsqlCommand(sql, connexion);
        await using var lecteur = await commande.ExecuteReaderAsync(cancellationToken);

        while (await lecteur.ReadAsync(cancellationToken))
        {
            lignes[lecteur.GetString(0)] = lecteur.GetInt64(1);
            octets[lecteur.GetString(0)] = lecteur.GetInt64(2);
        }

        return (lignes, octets);
    }
}
