using Npgsql;
using NutritionApi.Infrastructure.Observability;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;

namespace NutritionApi.Api.Extensions;

/// <summary>
/// Câblage OpenTelemetry — le socle commun aux traces, métriques et journaux (NTR-140).
/// </summary>
/// <remarks>
/// Ce socle ne mesure rien par lui-même. ASP.NET Core, <c>HttpClient</c>, Npgsql et
/// StackExchange.Redis émettent déjà leurs propres événements de mesure ; ce qui manquait était un
/// auditeur. Les instrumentations enregistrées ici s'y abonnent — aucun code de mesure n'est écrit
/// dans les controllers, les repositories ou les services.
/// <para>
/// La destination reste ouverte : l'exportateur OTLP parle le protocole standard, que les trois
/// candidats du volet 5 de l'epic consomment indifféremment. Changer de backend ne touchera que la
/// configuration, jamais l'instrumentation.
/// </para>
/// </remarks>
public static class ObservabilityExtensions
{
    /// <summary>Section de configuration portant les réglages d'observabilité.</summary>
    private const string SectionConfiguration = "OpenTelemetry";

    /// <summary>Nom porté par chaque mesure lorsque la configuration n'en impose pas d'autre.</summary>
    private const string NomServiceParDefaut = "nutrition-api";

    /// <summary>
    /// Préfixe des sondes de santé, exclues des traces : le kubelet les interroge en continu et
    /// noierait toute autre requête sous leur volume.
    /// </summary>
    private const string PrefixeSondes = "/health";

    /// <summary>
    /// Enregistre le fournisseur OpenTelemetry, les instrumentations automatiques et les
    /// exportateurs.
    /// </summary>
    /// <param name="services">Conteneur de services de l'application.</param>
    /// <param name="configuration">
    /// Configuration de l'application. La section <c>OpenTelemetry</c> porte trois clés :
    /// <c>ServiceName</c>, <c>OtlpEndpoint</c> et <c>ConsoleExporter</c>.
    /// </param>
    /// <param name="environnement">
    /// Environnement d'hébergement, publié comme attribut <c>deployment.environment</c> : sans lui,
    /// rien ne distingue une mesure de production d'une mesure de recette une fois collectées côte
    /// à côte.
    /// </param>
    /// <returns>Le conteneur, pour chaîner les appels.</returns>
    /// <remarks>
    /// À appeler <b>après</b> <c>AddInfrastructure</c> : l'instrumentation Redis résout le
    /// multiplexeur partagé depuis le conteneur, et celui-ci y est enregistré par la couche
    /// Infrastructure.
    /// </remarks>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environnement)
    {
        var section = configuration.GetSection(SectionConfiguration);

        var nomService = section["ServiceName"] ?? NomServiceParDefaut;
        var pointDeCollecte = section["OtlpEndpoint"];
        var versConsole = section.GetValue("ConsoleExporter", false);

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0";

        services.AddOpenTelemetry()
            // Identité commune à toutes les mesures, tous signaux confondus. Une mesure sans elle
            // est inexploitable dès qu'un collecteur reçoit plusieurs applications.
            .ConfigureResource(ressource => ressource
                .AddService(serviceName: nomService, serviceVersion: version)
                .AddAttributes([
                    new KeyValuePair<string, object>("deployment.environment", environnement.EnvironmentName)
                ]))

            .WithTracing(traces =>
            {
                traces
                    // Une trace par requête HTTP entrante, les sondes de santé exceptées.
                    .AddAspNetCoreInstrumentation(options =>
                        options.Filter = contexte =>
                            !contexte.Request.Path.StartsWithSegments(PrefixeSondes))
                    // Appels sortants — Keycloak et Open Food Facts passent par là.
                    .AddHttpClientInstrumentation()
                    // Chaque commande SQL devient une étape de la trace, avec sa durée.
                    .AddNpgsql()
                    // Idem pour les commandes Redis ; le multiplexeur vient du conteneur.
                    .AddRedisInstrumentation();

                AppliquerExportateurs(
                    versConsole, pointDeCollecte,
                    () => traces.AddConsoleExporter(),
                    adresse => traces.AddOtlpExporter(options => options.Endpoint = adresse));
            })

            .WithMetrics(metriques =>
            {
                metriques
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    // Saturation du pool de connexions PostgreSQL — publiée par Npgsql (NTR-138).
                    .AddNpgsqlInstrumentation()
                    // Taux de succès du cache et issue des jobs : les seules mesures que le projet
                    // produit lui-même, faute de bibliothèque qui les connaisse (NTR-138).
                    .AddMeter(InfrastructureMetrics.MeterName);

                AppliquerExportateurs(
                    versConsole, pointDeCollecte,
                    () => metriques.AddConsoleExporter(),
                    adresse => metriques.AddOtlpExporter(options => options.Endpoint = adresse));
            })

            // Les journaux passent par le même fournisseur, mais sans sortie console : le logger
            // d'ASP.NET Core écrit déjà là, et les doubler rendrait la console illisible. Leur
            // format et leur enrichissement relèvent de NTR-137.
            .WithLogging(journaux =>
                AppliquerExportateurs(
                    versConsole: false, pointDeCollecte,
                    versLaConsole: () => { },
                    adresse => journaux.AddOtlpExporter(options => options.Endpoint = adresse)));

        return services;
    }

    /// <summary>
    /// Branche les destinations retenues sur un signal. Aucune adresse de collecte configurée
    /// n'est une situation normale tant que le backend n'est pas choisi : les mesures sont alors
    /// produites puis abandonnées, sans erreur ni ralentissement notable.
    /// </summary>
    private static void AppliquerExportateurs(
        bool versConsole,
        string? pointDeCollecte,
        Action versLaConsole,
        Action<Uri> versLeCollecteur)
    {
        if (versConsole)
        {
            versLaConsole();
        }

        if (!string.IsNullOrWhiteSpace(pointDeCollecte))
        {
            versLeCollecteur(new Uri(pointDeCollecte));
        }
    }
}
