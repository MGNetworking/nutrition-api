namespace NutritionApi.Api.Tests.Level2;

using Microsoft.Extensions.DependencyInjection;
using NutritionApi.Api.Tests.Level2.Fixtures;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

/// <summary>
/// Vérifie le socle d'observabilité sur l'application réelle (NTR-140) : les fournisseurs de traces
/// et de métriques doivent exister, et porter l'identité du service.
/// </summary>
/// <remarks>
/// Le montage complet est ici indispensable, et non un excès de zèle : l'instrumentation Redis
/// résout le multiplexeur depuis le conteneur, ce qu'un test isolé sur une
/// <c>ServiceCollection</c> nue ne mettrait pas à l'épreuve. Une inversion d'ordre entre
/// <c>AddInfrastructure</c> et <c>AddObservability</c> échouerait au démarrage de la fabrique.
/// </remarks>
[Trait("Level", "2")]
[Collection(ApiCollection.Name)]
public class ObservabilityExtensionsTest
{
    private readonly ApiFactory _factory;

    public ObservabilityExtensionsTest(ApiFactory factory) => _factory = factory;

    [Fact]
    public void TracerProvider_IsRegistered()
    {
        var provider = _factory.Services.GetService<TracerProvider>();

        Assert.NotNull(provider);
    }

    [Fact]
    public void MeterProvider_IsRegistered()
    {
        var provider = _factory.Services.GetService<MeterProvider>();

        Assert.NotNull(provider);
    }

    [Fact]
    public void Resource_CarriesServiceName()
    {
        var attributs = _factory.Services.GetRequiredService<TracerProvider>().GetResource().Attributes;

        // Sans ce nom, un collecteur recevant plusieurs applications ne sait pas laquelle a émis.
        Assert.Contains(attributs, attribut =>
            attribut.Key == "service.name" && (string)attribut.Value == "nutrition-api");
    }

    [Fact]
    public void Resource_CarriesEnvironmentName()
    {
        var attributs = _factory.Services.GetRequiredService<TracerProvider>().GetResource().Attributes;

        // La fabrique monte l'application en environnement Testing : c'est ce nom qui doit remonter,
        // preuve que l'attribut vient bien de l'hôte et n'est pas une valeur écrite en dur.
        Assert.Contains(attributs, attribut =>
            attribut.Key == "deployment.environment" && (string)attribut.Value == "Testing");
    }
}
