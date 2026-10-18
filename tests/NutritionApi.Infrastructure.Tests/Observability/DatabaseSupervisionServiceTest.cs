namespace NutritionApi.Infrastructure.Tests.Observability;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NutritionApi.Infrastructure.Observability;
using System.Diagnostics.Metrics;
using System.Globalization;

/// <summary>
/// Vérifie la supervision de la base (NTR-141) : ce qui est journalisé à chaque relève, et ce qui
/// est publié comme mesure. La collecte elle-même interroge PostgreSQL — elle relève du niveau 3.
/// </summary>
/// <remarks>
/// Le service est construit directement plutôt que démarré asynchrone : c'est la restitution d'une
/// relève qui est vérifiée ici, pas la boucle de fond, qui n'ajoute que l'ordre des appels.
/// </remarks>
[Trait("Level", "1")]
public class DatabaseSupervisionServiceTest : IDisposable
{
    private const string InstrumentConnexions = "nutrition.db.connections";
    private const string InstrumentLignes = "nutrition.hangfire.rows";
    private const string InstrumentOctets = "nutrition.hangfire.bytes";

    /// <summary>Seuil d'avertissement par défaut : 512 Mo, en octets.</summary>
    private const long SeuilParDefautOctets = 536_870_912;

    private readonly ServiceProvider _services;
    private readonly IMeterFactory _fabrique;
    private readonly InfrastructureMetrics _metriques;
    private readonly Mock<ILogger<DatabaseSupervisionService>> _logger = new();

    public DatabaseSupervisionServiceTest()
    {
        _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        _fabrique = _services.GetRequiredService<IMeterFactory>();
        _metriques = new InfrastructureMetrics(_fabrique);
    }

    public void Dispose() => _services.Dispose();

    private DatabaseSupervisionService Sujet(long? seuilAlerteOctets = SeuilParDefautOctets)
    {
        var reglages = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=tests;Username=tests;Password=tests"
        };

        // Absent, le réglage laisse jouer la valeur par défaut du service.
        if (seuilAlerteOctets is { } seuil)
        {
            reglages["DatabaseObservability:HangfireSizeWarningThresholdBytes"] =
                seuil.ToString(CultureInfo.InvariantCulture);
        }

        return new DatabaseSupervisionService(
            _logger.Object, _metriques, new ConfigurationBuilder().AddInMemoryCollection(reglages).Build());
    }

    private static EtatBaseDonnees Releve(
        long actives,
        long bloquees,
        long inactives,
        params (string Table, long Lignes, long Octets)[] tables)
        => new(
            actives,
            bloquees,
            inactives,
            tables.ToDictionary(t => t.Table, t => t.Lignes),
            tables.ToDictionary(t => t.Table, t => t.Octets));

    private static long Valeur(MetricCollector<long> releves, string etiquette, string valeur)
        => releves.GetMeasurementSnapshot()
            .Single(m => (string?)m.Tags[etiquette] == valeur)
            .Value;

    private void VerifierJournalisation(LogLevel niveau, Func<string, bool> contenu, Times fois)
        => _logger.Verify(l => l.Log(
            niveau,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((message, _) => contenu(message.ToString()!)),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), fois);

    // ---------------------------------------------------------------------
    // Connexions — actives, bloquées, inactives
    // ---------------------------------------------------------------------

    [Fact]
    public void Rapporter_JournaliseLesConnexionsParEtat()
    {
        Sujet().Rapporter(Releve(actives: 12, bloquees: 4, inactives: 30));

        VerifierJournalisation(
            LogLevel.Information,
            message => message.Contains("12") && message.Contains("4") && message.Contains("30"),
            Times.Once());
    }

    [Fact]
    public void Rapporter_PublieLesConnexionsParEtat()
    {
        var releves = new MetricCollector<long>(_fabrique, InfrastructureMetrics.MeterName, InstrumentConnexions);

        Sujet().Rapporter(Releve(actives: 12, bloquees: 4, inactives: 30));
        releves.RecordObservableInstruments();

        Assert.Equal(12, Valeur(releves, "state", "active"));
        Assert.Equal(4, Valeur(releves, "state", "blocked"));
        Assert.Equal(30, Valeur(releves, "state", "idle"));
    }

    [Fact]
    public void Rapporter_SessionsBloquees_NePortentPlusLEtatWaiting()
    {
        var releves = new MetricCollector<long>(_fabrique, InfrastructureMetrics.MeterName, InstrumentConnexions);

        Sujet().Rapporter(Releve(actives: 1, bloquees: 4, inactives: 30));
        releves.RecordObservableInstruments();

        // « En attente » prêtait à confusion avec l'attente d'une connexion libre du pool, publiée
        // par Npgsql : le serveur, lui, bloque une session sur un verrou ou une ressource.
        Assert.DoesNotContain(releves.GetMeasurementSnapshot(), m => (string?)m.Tags["state"] == "waiting");
    }

    // ---------------------------------------------------------------------
    // Volume des tables hangfire — lignes estimées et taille disque
    // ---------------------------------------------------------------------

    [Fact]
    public void Rapporter_PublieLeNombreDeLignesEstimeDeChaqueTableHangfire()
    {
        var releves = new MetricCollector<long>(_fabrique, InfrastructureMetrics.MeterName, InstrumentLignes);

        Sujet().Rapporter(Releve(0, 0, 0, ("job", 150, 4096), ("state", 900, 8192), ("counter", 3, 1024)));
        releves.RecordObservableInstruments();

        // Les trois tables grossissent sans purge : chacune porte sa propre série.
        Assert.Equal(150, Valeur(releves, "table", "job"));
        Assert.Equal(900, Valeur(releves, "table", "state"));
        Assert.Equal(3, Valeur(releves, "table", "counter"));
    }

    [Fact]
    public void Rapporter_PublieLaTailleDisqueDeChaqueTableHangfire()
    {
        var releves = new MetricCollector<long>(_fabrique, InfrastructureMetrics.MeterName, InstrumentOctets);

        Sujet().Rapporter(Releve(0, 0, 0, ("job", 150, 4096), ("state", 900, 8192), ("counter", 3, 1024)));
        releves.RecordObservableInstruments();

        // Deux séries distinctes : les lignes arrivent du catalogue PostgreSQL, la taille disque en
        // est une autre mesure — c'est elle que le seuil d'avertissement observe.
        Assert.Equal(4096, Valeur(releves, "table", "job"));
        Assert.Equal(8192, Valeur(releves, "table", "state"));
        Assert.Equal(1024, Valeur(releves, "table", "counter"));
    }

    // ---------------------------------------------------------------------
    // Avertissement — sur la taille disque seule
    // ---------------------------------------------------------------------

    [Fact]
    public void Rapporter_TableAtteignantLeSeuil_JournaliseUnAvertissement()
    {
        Sujet(seuilAlerteOctets: 100).Rapporter(Releve(0, 0, 0, ("state", 1, 900)));

        // « Avant que ce soit un incident » : dépasser le seuil doit se voir sans attendre.
        VerifierJournalisation(LogLevel.Warning, message => message.Contains("state"), Times.Once());
    }

    [Fact]
    public void Rapporter_TablesSousLeSeuil_NAvertitPas()
    {
        Sujet(seuilAlerteOctets: 100).Rapporter(Releve(0, 0, 0, ("job", 1, 99), ("state", 1, 50), ("counter", 1, 1)));

        // Sous le seuil, la relève reste une information : avertir à chaque cycle rendrait
        // l'avertissement inaudible le jour où il compte.
        VerifierJournalisation(LogLevel.Warning, _ => true, Times.Never());
    }

    [Fact]
    public void Rapporter_TableVolumineuseEnLignes_MaisSousLeSeuilDeTaille_NAvertitPas()
    {
        Sujet(seuilAlerteOctets: 1_000_000)
            .Rapporter(Releve(0, 0, 0, ("job", 50_000_000, 500_000)));

        // Le seuil porte sur la taille disque seule : une table à beaucoup de lignes mais compacte
        // est normale, et un avertissement fondé sur les lignes inviterait à purger sans raison.
        VerifierJournalisation(LogLevel.Warning, _ => true, Times.Never());
    }

    [Theory]
    [InlineData(SeuilParDefautOctets - 1, false)]
    [InlineData(SeuilParDefautOctets, true)]
    public void Rapporter_SansSeuilConfigure_AvertitAPartirDe512Mo(long tailleOctets, bool attendu)
    {
        Sujet(seuilAlerteOctets: null).Rapporter(Releve(0, 0, 0, ("state", 1, tailleOctets)));

        // 512 Mo : quand Hangfire purge normalement, ces tables restent à quelques dizaines de Mo —
        // les atteindre est déjà anormal, et le seuil doit valoir sans qu'on l'ait configuré.
        VerifierJournalisation(LogLevel.Warning, _ => true, attendu ? Times.Once() : Times.Never());
    }

    // ---------------------------------------------------------------------
    // Cas d'erreur — configuration
    // ---------------------------------------------------------------------

    [Fact]
    public void Constructeur_ChaineDeConnexionAbsente_EstRefusee()
    {
        var configuration = new ConfigurationBuilder().Build();

        Assert.Throws<InvalidOperationException>(
            () => new DatabaseSupervisionService(_logger.Object, _metriques, configuration));
    }
}
