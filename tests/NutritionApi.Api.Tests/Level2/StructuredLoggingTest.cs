namespace NutritionApi.Api.Tests.Level2;

using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NutritionApi.Api.Tests.Level2.Fixtures;
using Serilog.Core;
using Serilog.Events;

/// <summary>
/// Vérifie que Serilog est bien le fournisseur de journalisation de l'application, et que ce qu'il
/// écrit est exploitable (NTR-137).
/// </summary>
/// <remarks>
/// Le montage complet est nécessaire : ce qui est éprouvé ici n'est pas le comportement de Serilog,
/// mais son <b>branchement</b> — la configuration lue depuis <c>appsettings.json</c> et la
/// substitution des fournisseurs d'ASP.NET Core.
/// </remarks>
[Trait("Level", "2")]
[Collection(ApiCollection.Name)]
public class StructuredLoggingTest
{
    private readonly ApiFactory _factory;

    public StructuredLoggingTest(ApiFactory factory) => _factory = factory;

    /// <summary>
    /// Destination de test. Serilog la découvre par <c>ReadFrom.Services</c>, qui recueille dans le
    /// conteneur les destinations enregistrées — aucun paquet supplémentaire n'est donc nécessaire.
    /// </summary>
    private sealed class Capture : ILogEventSink
    {
        private readonly List<LogEvent> _evenements = [];

        public void Emit(LogEvent logEvent)
        {
            lock (_evenements)
            {
                _evenements.Add(logEvent);
            }
        }

        public List<LogEvent> Relever()
        {
            lock (_evenements)
            {
                return [.. _evenements];
            }
        }
    }

    /// <summary>Monte l'application avec une destination de capture branchée sur Serilog.</summary>
    private (HttpClient Client, Capture Journaux) AvecCapture()
    {
        var capture = new Capture();

        var fabrique = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddSingleton<ILogEventSink>(capture)));

        return (fabrique.CreateClient(), capture);
    }

    [Fact]
    public async Task Request_IsLoggedThroughSerilog()
    {
        var (client, journaux) = AvecCapture();

        await client.GetAsync("/api/v1/users/me");

        // Si les fournisseurs d'ASP.NET Core étaient encore en place, cette destination ne recevrait
        // rien : c'est la preuve que la substitution a eu lieu.
        Assert.NotEmpty(journaux.Relever());
    }

    [Fact]
    public async Task LogEntries_CarryTheTraceId()
    {
        var (client, journaux) = AvecCapture();

        await client.GetAsync("/api/v1/users/me");

        var entrees = journaux.Relever();

        // L'identifiant de trace relie l'entrée de journal à la trace de la requête et à celui
        // publié au client en cas d'erreur. Les trois se recoupent enfin.
        //
        // Toutes les entrées ne peuvent pas le porter : celles du démarrage et des services de fond
        // n'appartiennent à aucune requête, donc à aucune trace. C'est bien qu'au moins une entrée
        // le porte qui prouve le branchement.
        Assert.Contains(entrees, entree => entree.TraceId is not null && entree.TraceId != default);
    }
}
