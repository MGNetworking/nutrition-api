namespace NutritionApi.Infrastructure.Tests.Persistence;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Npgsql;
using NutritionApi.Application.Exceptions;
using NutritionApi.Infrastructure.Observability;
using NutritionApi.Infrastructure.Persistence.Interceptors;
using System.Diagnostics.Metrics;
using System.Net.Sockets;

/// <summary>
/// Vérifie le suivi des connexions rejetées (NTR-141) : une tentative d'ouverture refusée est
/// comptée comme telle, sans cesser d'être traduite en indisponibilité.
/// </summary>
/// <remarks>
/// Seule la décision est éprouvée, pas son branchement sur EF Core : elle est extraite dans une
/// méthode publique pour être vérifiable sans base réelle — même approche que
/// <c>DatabaseExceptionInterceptor.Translate</c>.
/// </remarks>
[Trait("Level", "1")]
public class DatabaseConnectionExceptionInterceptorTest : IDisposable
{
    private const string InstrumentConnexionsRejetees = "nutrition.db.connections.rejected";

    private readonly ServiceProvider _services;
    private readonly IMeterFactory _fabrique;
    private readonly InfrastructureMetrics _metriques;

    public DatabaseConnectionExceptionInterceptorTest()
    {
        _services = new ServiceCollection().AddMetrics().BuildServiceProvider();
        _fabrique = _services.GetRequiredService<IMeterFactory>();
        _metriques = new InfrastructureMetrics(_fabrique);
    }

    public void Dispose() => _services.Dispose();

    private MetricCollector<long> ObserverLesRejets()
        => new(_fabrique, InfrastructureMetrics.MeterName, InstrumentConnexionsRejetees);

    private static NpgsqlException BaseInjoignable()
        => new("connexion refusée", new SocketException(10061));

    [Fact]
    public void ConnexionRefusee_EstCompteeEtTraduite()
    {
        var releves = ObserverLesRejets();
        var interceptor = new DatabaseConnectionExceptionInterceptor(_metriques);

        var traduite = interceptor.TraduireEtCompter(BaseInjoignable());

        Assert.IsType<ServiceUnavailableException>(traduite);
        Assert.Equal(1, releves.GetMeasurementSnapshot().Sum(m => m.Value));
    }

    [Fact]
    public void ConnexionRefusee_Repetee_IncidenteLeCompteur()
    {
        var releves = ObserverLesRejets();
        var interceptor = new DatabaseConnectionExceptionInterceptor(_metriques);

        interceptor.TraduireEtCompter(BaseInjoignable());
        interceptor.TraduireEtCompter(BaseInjoignable());

        Assert.Equal(2, releves.GetMeasurementSnapshot().Sum(m => m.Value));
    }

    [Fact]
    public void EchecNonTraduit_NeCompteRien()
    {
        var releves = ObserverLesRejets();
        var interceptor = new DatabaseConnectionExceptionInterceptor(_metriques);

        var traduite = interceptor.TraduireEtCompter(new InvalidOperationException("autre chose"));

        // Ce qui n'est pas une indisponibilité de base n'est pas une connexion rejetée.
        Assert.Null(traduite);
        Assert.Empty(releves.GetMeasurementSnapshot());
    }

    [Fact]
    public void SansMetriques_LaTraductionResteAssuree()
    {
        // La dépendance est facultative : le branchement des intercepteurs hors conteneur de
        // mesures — un test d'intégration, par exemple — ne doit pas en dépendre.
        var interceptor = new DatabaseConnectionExceptionInterceptor();

        Assert.IsType<ServiceUnavailableException>(interceptor.TraduireEtCompter(BaseInjoignable()));
    }
}
