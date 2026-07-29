namespace NutritionApi.Infrastructure;

using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Infrastructure.Caching;
using NutritionApi.Infrastructure.ExternalServices.Keycloak;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Infrastructure.Jobs.RgpdPurge;
using NutritionApi.Infrastructure.Scheduling;
using NutritionApi.Infrastructure.Persistence;
using NutritionApi.Infrastructure.Persistence.Interceptors;
using NutritionApi.Infrastructure.Persistence.Repositories;
using StackExchange.Redis;

public static class InfrastructureExtensions
{
    /// <summary>Enregistre le contexte EF Core, l'unité de travail et les repositories.</summary>
    /// <param name="services">Collection de services de l'application.</param>
    /// <param name="configuration">Configuration — la chaîne <c>DefaultConnection</c> est requise.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // L'intercepteur traduit les échecs PostgreSQL en exceptions applicatives, en un seul point
        // pour toutes les commandes : violation d'unicité en conflit, base injoignable en
        // indisponibilité. Sans lui, la couche API devrait connaître Npgsql pour les distinguer.
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention()
                   .AddInterceptors(new DatabaseExceptionInterceptor()));

        // L'unité de travail est le DbContext lui-même — même instance dans la portée de la requête
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDietPlanRepository, DietPlanRepository>();
        services.AddScoped<IDietRepository, DietRepository>();
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IFoodItemRepository, FoodItemRepository>();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<ISavedFoodItemRepository, SavedFoodItemRepository>();

        // ── Redis ─────────────────────────────────────────────────────────────
        // Le multiplexeur est coûteux à créer et thread-safe : une seule instance partagée.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration["Redis:ConnectionString"]!);

            // Sans cela, une instance Redis indisponible empêcherait l'API de démarrer :
            // la connexion est retentée en arrière-plan au lieu d'échouer immédiatement.
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });

        services.AddScoped<IFoodCacheService, RedisFoodCacheService>();

        // ── Hangfire ──────────────────────────────────────────────────────────
        // Jobs planifiés persistés dans PostgreSQL (schéma dédié, tables créées au
        // démarrage). Le serveur d'exécution tourne dans le process de l'API.
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));

        // Lance la boucle de fond dans l'API.
        // C'est elle qui, toutes les quelques secondes, regarde en base si un job est dû et l'exécute.
        services.AddHangfireServer();

        // Déclare les jobs récurrents au démarrage. Hosted service et non appel direct dans
        // Program.cs : un test d'intégration peut ainsi le retirer avec le serveur Hangfire,
        // et un storage injoignable n'empêche plus l'API de démarrer.
        services.AddHostedService<RecurringJobRegistrationService>();

        // Supervision des jobs planifiés — lit l'état des recurring jobs dans hangfire.hash
        services.AddScoped<IJobMonitoringService, JobMonitoringService>();

        // Import Open Food Facts — téléchargement du dump + alimentation du catalogue par lots
        services.AddHttpClient<IOffDumpReader, OffDumpReader>();
        services.AddScoped<IOffImportJob, OffImportJob>();

        // Purge RGPD — suppression des comptes dont la grace period est expirée
        services.AddScoped<IRgpdPurgeJob, RgpdPurgeJob>();

        // ── Keycloak Admin ────────────────────────────────────────────────────
        // Administration des comptes du realm : désactivation pendant la grace period RGPD,
        // suppression définitive par le job de purge.
        services.Configure<KeycloakAdminOptions>(configuration.GetSection(KeycloakAdminOptions.SectionName));

        // Horloge injectable — le fournisseur de jetons l'utilise pour évaluer l'expiration.
        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient(KeycloakTokenProvider.HttpClientName)
                .AddStandardResilienceHandler();

        // Le jeton de service est mémorisé entre les appels : le fournisseur doit survivre aux
        // requêtes, d'où le singleton et son client HTTP résolu par la fabrique.
        services.AddSingleton<IKeycloakTokenProvider>(sp => new KeycloakTokenProvider(
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(KeycloakTokenProvider.HttpClientName),
            sp.GetRequiredService<IOptions<KeycloakAdminOptions>>(),
            sp.GetRequiredService<TimeProvider>()));

        // Le handler standard retente les 5xx, 408 et 429 — le 401 reste géré par le service,
        // seul capable d'invalider le jeton avant de rejouer.
        services.AddHttpClient<IKeycloakAdminService, KeycloakAdminService>(KeycloakAdminService.HttpClientName)
                .AddStandardResilienceHandler();

        return services;
    }
}
