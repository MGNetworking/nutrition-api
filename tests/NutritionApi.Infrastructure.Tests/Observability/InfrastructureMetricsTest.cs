namespace NutritionApi.Infrastructure.Tests.Observability;

using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.DependencyInjection;
using NutritionApi.Infrastructure.Observability;
using System.Diagnostics.Metrics;

/// <summary>
/// Vérifie les deux instruments que le projet produit lui-même (NTR-138) : ce qu'ils comptent, et
/// les étiquettes sans lesquelles la mesure serait inexploitable.
/// </summary>
/// <remarks>
/// Une fabrique par instance de test, et non un compteur statique partagé : deux tests qui mesurent
/// la même chose se liraient sinon les relevés l'un de l'autre.
/// </remarks>
[Trait("Level", "1")]
public class InfrastructureMetricsTest : IDisposable
{
    private readonly ServiceProvider _services;
    private readonly IMeterFactory _fabrique;

    public InfrastructureMetricsTest()
    {
        _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        _fabrique = _services.GetRequiredService<IMeterFactory>();
    }

    public void Dispose() => _services.Dispose();

    /// <summary>Crée le sujet et le collecteur branché sur l'instrument nommé.</summary>
    private (InfrastructureMetrics Metriques, MetricCollector<T> Releves) Observer<T>(string instrument)
        where T : struct
    {
        var metriques = new InfrastructureMetrics(_fabrique);
        var releves = new MetricCollector<T>(_fabrique, InfrastructureMetrics.MeterName, instrument);

        return (metriques, releves);
    }

    [Theory]
    [InlineData("hit")]
    [InlineData("miss")]
    [InlineData("failure")]
    public void CacheLookup_IsTaggedWithItsOutcome(string issueAttendue)
    {
        var (metriques, releves) = Observer<long>("nutrition.cache.lookups");

        switch (issueAttendue)
        {
            case "hit": metriques.CacheHit(); break;
            case "miss": metriques.CacheMiss(); break;
            default: metriques.CacheFailure(); break;
        }

        var mesure = Assert.Single(releves.GetMeasurementSnapshot());

        Assert.Equal(1, mesure.Value);
        Assert.Equal(issueAttendue, mesure.Tags["outcome"]);
    }

    [Fact]
    public void CacheLookups_AccumulateOnASingleSeries()
    {
        var (metriques, releves) = Observer<long>("nutrition.cache.lookups");

        metriques.CacheHit();
        metriques.CacheHit();
        metriques.CacheMiss();

        // Une seule série étiquetée, non trois compteurs : c'est ce qui permet de calculer le taux
        // de succès à la lecture. Trois succès et un défaut donnent bien quatre relevés au total.
        var mesures = releves.GetMeasurementSnapshot();

        Assert.Equal(3, mesures.Count);
        Assert.Equal(2, mesures.Count(m => (string?)m.Tags["outcome"] == "hit"));
        Assert.Equal(1, mesures.Count(m => (string?)m.Tags["outcome"] == "miss"));
    }

    [Fact]
    public void JobExecute_RecordsDurationInSeconds()
    {
        var (metriques, releves) = Observer<double>("nutrition.job.duration");

        metriques.JobExecute("OffImportJob", "success", TimeSpan.FromMilliseconds(1500));

        var mesure = Assert.Single(releves.GetMeasurementSnapshot());

        // L'unité déclarée est la seconde : enregistrer des millisecondes rendrait tout seuil faux
        // d'un facteur mille, sans que rien ne le signale.
        Assert.Equal(1.5, mesure.Value);
        Assert.Equal("OffImportJob", mesure.Tags["job"]);
        Assert.Equal("success", mesure.Tags["outcome"]);
    }

    [Fact]
    public void JobExecute_DistinguishesFailureFromSuccess()
    {
        var (metriques, releves) = Observer<double>("nutrition.job.duration");

        metriques.JobExecute("RgpdPurgeJob", "failure", TimeSpan.FromSeconds(2));

        var mesure = Assert.Single(releves.GetMeasurementSnapshot());

        // Un job qui échoue chaque nuit doit se voir : sans cette étiquette, il se confondrait avec
        // un job qui réussit vite.
        Assert.Equal("failure", mesure.Tags["outcome"]);
    }
}
