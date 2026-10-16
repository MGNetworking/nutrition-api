namespace NutritionApi.Infrastructure.Observability;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

/// <summary>Relève de l'état de la base : connexions ouvertes et taille des tables Hangfire.</summary>
/// <param name="ConnexionsActives">Connexions en train d'exécuter une requête.</param>
/// <param name="ConnexionsEnAttente">Connexions actives bloquées, en attente d'une ressource.</param>
/// <param name="ConnexionsInactives">Connexions ouvertes mais au repos.</param>
/// <param name="TablesHangfire">Nombre de lignes par table du schéma <c>hangfire</c>.</param>
public sealed record EtatBaseDonnees(
    long ConnexionsActives,
    long ConnexionsEnAttente,
    long ConnexionsInactives,
    IReadOnlyDictionary<string, long> TablesHangfire);

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
/// l'état des connexions — actives, en attente, inactives — tel que le serveur le voit, lu dans
/// <c>pg_stat_activity</c>. Complémentaire des mesures du pool publiées par Npgsql (NTR-138) :
/// celles-ci décrivent les connexions du processus, celles-là les sessions reçues par le serveur.
/// </item>
/// <item>
/// la taille des tables <c>job</c>, <c>state</c> et <c>counter</c>. Hangfire n'embarque aucune purge
/// configurée : ces tables grossissent indéfiniment, et une taille anormale se voit d'autant mieux
/// qu'elle est publiée comme une tendance — c'est le rôle de la jauge
/// <c>nutrition.hangfire.rows</c>.
/// </item>
/// </list>
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

    /// <summary>Nombre de lignes au-delà duquel une table Hangfire déclenche un avertissement.</summary>
    public const long SeuilAlerteParDefaut = 1_000_000;

    private readonly ILogger<DatabaseSupervisionService> _logger;
    private readonly InfrastructureMetrics _metriques;
    private readonly string _connectionString;
    private readonly TimeSpan _periode;
    private readonly long _seuilAlerte;

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
        _seuilAlerte = section.GetValue("HangfireRowsWarningThreshold", SeuilAlerteParDefaut);
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
    /// <returns>La relève : connexions par état et taille des tables Hangfire.</returns>
    /// <exception cref="NpgsqlException">La base est injoignable.</exception>
    public async Task<EtatBaseDonnees> CollecterAsync(CancellationToken cancellationToken = default)
    {
        await using var connexion = new NpgsqlConnection(_connectionString);
        await connexion.OpenAsync(cancellationToken);

        var connexions = await LireConnexionsAsync(connexion, cancellationToken);
        var tables = await LireTablesHangfireAsync(connexion, cancellationToken);

        return new EtatBaseDonnees(connexions.Actives, connexions.EnAttente, connexions.Inactives, tables);
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
        _metriques.Connexions("waiting", releve.ConnexionsEnAttente);
        _metriques.Connexions("idle", releve.ConnexionsInactives);

        _logger.LogInformation(
            "Connexions PostgreSQL — actives : {Actives}, en attente : {EnAttente}, inactives : {Inactives}.",
            releve.ConnexionsActives,
            releve.ConnexionsEnAttente,
            releve.ConnexionsInactives);

        foreach (var (table, lignes) in releve.TablesHangfire)
        {
            _metriques.TailleTableHangfire(table, lignes);

            _logger.LogInformation("Table hangfire.{Table} : {Lignes} lignes.", table, lignes);

            if (lignes >= _seuilAlerte)
            {
                _logger.LogWarning(
                    "Table hangfire.{Table} volumineuse : {Lignes} lignes (seuil {Seuil}) — purge à prévoir.",
                    table,
                    lignes,
                    _seuilAlerte);
            }
        }
    }

    /// <summary>
    /// Compte les sessions reçues par le serveur, par état. « En attente » désigne une session active
    /// bloquée sur une ressource — un verrou, un entrée-sortie —, à distinguer d'une session au repos.
    /// </summary>
    private static async Task<(long Actives, long EnAttente, long Inactives)> LireConnexionsAsync(
        NpgsqlConnection connexion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                count(*) filter (where state = 'active' and wait_event_type is null)     as actives,
                count(*) filter (where state = 'active' and wait_event_type is not null) as en_attente,
                count(*) filter (where state = 'idle')                                   as inactives
            from pg_stat_activity
            where datname = current_database();
            """;

        await using var commande = new NpgsqlCommand(sql, connexion);
        await using var lecteur = await commande.ExecuteReaderAsync(cancellationToken);

        if (!await lecteur.ReadAsync(cancellationToken))
            return (0, 0, 0);

        return (lecteur.GetInt64(0), lecteur.GetInt64(1), lecteur.GetInt64(2));
    }

    /// <summary>
    /// Compte les lignes des trois tables que Hangfire ne purge pas de lui-même. Le schéma
    /// <c>hangfire</c> est créé par Hangfire au démarrage : son absence fait échouer la requête, que
    /// l'appelant journalise et rejoue.
    /// </summary>
    private static async Task<IReadOnlyDictionary<string, long>> LireTablesHangfireAsync(
        NpgsqlConnection connexion,
        CancellationToken cancellationToken)
    {
        const string sql = """
            select
                (select count(*) from hangfire.job)     as job,
                (select count(*) from hangfire.state)   as state,
                (select count(*) from hangfire.counter) as counter;
            """;

        await using var commande = new NpgsqlCommand(sql, connexion);
        await using var lecteur = await commande.ExecuteReaderAsync(cancellationToken);

        if (!await lecteur.ReadAsync(cancellationToken))
            return new Dictionary<string, long>();

        return new Dictionary<string, long>
        {
            ["job"] = lecteur.GetInt64(0),
            ["state"] = lecteur.GetInt64(1),
            ["counter"] = lecteur.GetInt64(2)
        };
    }
}
