namespace NutritionApi.Api.Tests.Level2.Fixtures;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

/// <summary>
/// Fabrique des tests de niveau 2 : l'application réelle, dont tout ce qui franchit les interfaces
/// d'Application est remplacé par une doublure.
/// </summary>
/// <remarks>
/// Restent réels : routage, liaison de modèle, middlewares, autorisation, sérialisation et codes de
/// statut — c'est-à-dire ce que ce niveau doit prouver. PostgreSQL, Redis et Keycloak appartiennent
/// au niveau 3 (NTR-28) : les remplacer ici n'est pas un raccourci, c'est la frontière.
/// </remarks>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Identifiant Keycloak utilisé par défaut par <see cref="CreateAuthenticatedClient"/>.</summary>
    public const string DefaultSubject = "11111111-1111-1111-1111-111111111111";

    public Mock<IUserRepository> Users { get; } = new();
    public Mock<IDietPlanRepository> DietPlans { get; } = new();
    public Mock<IDietRepository> Diets { get; } = new();
    public Mock<IMealRepository> Meals { get; } = new();
    public Mock<IFoodItemRepository> FoodItems { get; } = new();
    public Mock<IWeightEntryRepository> WeightEntries { get; } = new();
    public Mock<ISavedFoodItemRepository> SavedFoodItems { get; } = new();
    public Mock<IUnitOfWork> UnitOfWork { get; } = new();
    public Mock<IFoodCacheService> FoodCache { get; } = new();
    public Mock<IKeycloakAdminService> KeycloakAdmin { get; } = new();
    public Mock<IJobMonitoringService> JobMonitoring { get; } = new();

    /// <summary>Utilisateur retourné par défaut à la résolution d'identité.</summary>
    public User CurrentUser { get; } = new(
        keycloakId: DefaultSubject,
        birthDate: new DateOnly(1992, 3, 15),
        gender: Gender.Male,
        activityLevel: ActivityLevel.Sedentary,
        height: 180,
        allergies: [],
        dietaryPreferences: []);

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Valeurs factices : rien ici n'est joignable, et rien n'a besoin de l'être. Elles existent
        // parce que plusieurs enregistrements analysent ces chaînes au démarrage — une valeur nulle
        // ferait échouer AddInfrastructure avant même le premier test.
        // Les délais courts font échouer vite plutôt qu'attendre, si un appel passait malgré tout.
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Database=tests;Username=tests;Password=tests;Timeout=1;Command Timeout=1");
        builder.UseSetting("Redis:ConnectionString", "localhost:6379,abortConnect=false,connectTimeout=100");
        builder.UseSetting("Keycloak:Authority", "https://keycloak.tests/realms/tests");
        builder.UseSetting("Keycloak:RequireHttpsMetadata", "false");

        builder.ConfigureTestServices(services =>
        {
            // Emporte le serveur Hangfire et l'enregistrement des jobs récurrents : tous deux sont
            // des hosted services, et tous deux exigeraient PostgreSQL au démarrage.
            services.RemoveAll<IHostedService>();

            Substitute(services, Users.Object);
            Substitute(services, DietPlans.Object);
            Substitute(services, Diets.Object);
            Substitute(services, Meals.Object);
            Substitute(services, FoodItems.Object);
            Substitute(services, WeightEntries.Object);
            Substitute(services, SavedFoodItems.Object);
            Substitute(services, UnitOfWork.Object);
            Substitute(services, FoodCache.Object);
            Substitute(services, KeycloakAdmin.Object);
            Substitute(services, JobMonitoring.Object);

            services.AddAuthentication(TestAuthHandler.SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });

        // UserResolutionMiddleware interroge ce dépôt sur chaque requête authentifiée et renvoie 401
        // si l'utilisateur est introuvable. Sans cette valeur par défaut, tous les tests échoueraient
        // sur un 401 sans rapport avec ce qu'ils vérifient.
        Users.Setup(r => r.GetByKeycloakIdAsync(DefaultSubject)).ReturnsAsync(CurrentUser);
    }

    /// <summary>Remplace l'enregistrement d'un service par une doublure.</summary>
    /// <typeparam name="TService">Interface de frontière à substituer.</typeparam>
    /// <param name="services">Collection de services du test.</param>
    /// <param name="instance">Doublure à enregistrer.</param>
    private static void Substitute<TService>(IServiceCollection services, TService instance)
        where TService : class
    {
        services.RemoveAll<TService>();
        services.AddScoped(_ => instance);
    }

    /// <summary>Crée un client dont les requêtes portent une identité authentifiée.</summary>
    /// <param name="subject">Identifiant Keycloak — <see cref="DefaultSubject"/> par défaut.</param>
    /// <param name="roles">Rôles à porter, par exemple <c>admin</c>.</param>
    /// <returns>Un client HTTP prêt à appeler l'API.</returns>
    public HttpClient CreateAuthenticatedClient(string? subject = null, params string[] roles)
    {
        var client = CreateClient();

        client.DefaultRequestHeaders.Add(TestAuthHandler.SubjectHeader, subject ?? DefaultSubject);

        if (roles.Length > 0)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));

        return client;
    }

    /// <summary>Crée un client sans identité — pour vérifier les 401.</summary>
    /// <returns>Un client HTTP anonyme.</returns>
    public HttpClient CreateAnonymousClient() => CreateClient();

    /// <summary>
    /// Efface l'historique d'appels de toutes les doublures, sans toucher à leurs configurations.
    /// </summary>
    /// <remarks>
    /// La fabrique étant partagée par toute la suite, <c>Verify(…, Times.Never)</c> compterait sinon
    /// les appels de <b>tous</b> les tests précédents : un test verrait échouer une vérification à
    /// cause d'un autre. À appeler dans le constructeur de chaque classe de tests — xUnit
    /// l'instancie avant chaque méthode, la remise à zéro est donc automatique.
    /// </remarks>
    public void ResetInvocations()
    {
        Users.Invocations.Clear();
        DietPlans.Invocations.Clear();
        Diets.Invocations.Clear();
        Meals.Invocations.Clear();
        FoodItems.Invocations.Clear();
        WeightEntries.Invocations.Clear();
        SavedFoodItems.Invocations.Clear();
        UnitOfWork.Invocations.Clear();
        FoodCache.Invocations.Clear();
        KeycloakAdmin.Invocations.Clear();
        JobMonitoring.Invocations.Clear();
    }
}
