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
    private const string InstrumentTables = "nutrition.hangfire.rows";

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

    private DatabaseSupervisionService Sujet(long seuilAlerte = 1_000_000)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=tests;Username=tests;Password=tests",
                ["DatabaseObservability:HangfireRowsWarningThreshold"] = seuilAlerte.ToString(CultureInfo.InvariantCulture)
            })
            .Build();

        return new DatabaseSupervisionService(_logger.Object, _metriques, configuration);
    }

    private static EtatBaseDonnees Releve(
        long actives,
        long enAttente,
        long inactives,
        params (string Table, long Lignes)[] tables)
        => new(actives, enAttente, inactives, tables.ToDictionary(t => t.Table, t => t.Lignes));

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
    // Connexions — actives, en attente, inactives
    // ---------------------------------------------------------------------

    [Fact]
    public void Rapporter_JournaliseLesConnexionsParEtat()
    {
        Sujet().Rapporter(Releve(actives: 12, enAttente: 4, inactives: 30));

        VerifierJournalisation(
            LogLevel.Information,
            message => message.Contains("12") && message.Contains("4") && message.Contains("30"),
            Times.Once());
    }

    [Fact]
    public void Rapporter_PublieLesConnexionsParEtat()
    {
        var releves = new MetricCollector<long>(_fabrique, InfrastructureMetrics.MeterName, InstrumentConnexions);

        Sujet().Rapporter(Releve(actives: 12, enAttente: 4, inactives: 30));
        releves.RecordObservableInstruments();

        Assert.Equal(12, Valeur(releves, "state", "active"));
        Assert.Equal(4, Valeur(releves, "state", "waiting"));
        Assert.Equal(30, Valeur(releves, "state", "idle"));
    }

    // ---------------------------------------------------------------------
    // Croissance des tables hangfire
    // ---------------------------------------------------------------------

    [Fact]
    public void Rapporter_PublieLaTailleDeChaqueTableHangfire()
    {
        var releves = new MetricCollector<long>(_fabrique, InfrastructureMetrics.MeterName, InstrumentTables);

        Sujet().Rapporter(Releve(0, 0, 0, ("job", 150), ("state", 900), ("counter", 3)));
        releves.RecordObservableInstruments();

        // Les trois tables grossissent sans purge : chacune porte sa propre série.
        Assert.Equal(150, Valeur(releves, "table", "job"));
        Assert.Equal(900, Valeur(releves, "table", "state"));
        Assert.Equal(3, Valeur(releves, "table", "counter"));
    }

    [Fact]
    public void Rapporter_TableVolumineuse_JournaliseUnAvertissement()
    {
        Sujet(seuilAlerte: 100).Rapporter(Releve(0, 0, 0, ("state", 900)));

        // « Avant que ce soit un incident » : dépasser le seuil doit se voir sans attendre.
        VerifierJournalisation(LogLevel.Warning, message => message.Contains("state"), Times.Once());
    }

    [Fact]
    public void Rapporter_TablesSousLeSeuil_NAvertitPas()
    {
        Sujet(seuilAlerte: 100).Rapporter(Releve(0, 0, 0, ("job", 99), ("state", 50), ("counter", 1)));

        // Sous le seuil, la relève reste une information : avertir à chaque cycle rendrait
        // l'avertissement inaudible le jour où il compte.
        VerifierJournalisation(LogLevel.Warning, _ => true, Times.Never());
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
