namespace NutritionApi.Api.Tests.Level2;

using Moq;
using NutritionApi.Api.Tests.Level2.Fixtures;
using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>AdminController</c> — NTR-106. Couvre le tableau de bord, la santé du système et la gestion des templates.
/// </summary>
[Trait("Level", "2")]
[Collection(ApiCollection.Name)]
public class AdminIntegrationTest
{
    private const string Dashboard = "/api/v1/admin/dashboard";
    private const string Health = "/api/v1/admin/system/health";
    private const string Templates = "/api/v1/admin/diet-plans/templates";

    private readonly ApiFactory _factory;

    public AdminIntegrationTest(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetInvocations();
    }

    private HttpClient Admin => _factory.CreateAuthenticatedClient(roles: "admin");

    private static CreateDietPlanRequest RequeteCreation()
        => new("Équilibre méditerranéen", DietType.Balanced, Goal.Maintenance, 70f, new MacroDistributionDto(30, 40, 30));

    private static UpdateDietPlanRequest RequeteMiseAJour(string nom = "Équilibre révisé")
        => new(nom, DietType.Balanced, Goal.Maintenance, 70f, new MacroDistributionDto(30, 40, 30));

    /// <summary>Crée un plan, template ou personnel — la distinction décide du 404 sur les routes admin.</summary>
    private static DietPlan Plan(bool template)
        => template
            ? new DietPlan(null, "Template", true, DietType.Balanced, Goal.Maintenance, 70f, new MacroDistribution(30, 40, 30))
            : new DietPlan(Guid.NewGuid(), "Personnel", false, DietType.Balanced, Goal.Maintenance, 70f, new MacroDistribution(30, 40, 30));

    // ── Tableau de bord ────────────────────────────────

    [Fact]
    public async Task GetAdminDashboard_ShouldReturn403_WhenUserIsNotAdmin()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Dashboard);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task GetAdminDashboard_ShouldReturn200WithKpis_WhenUserIsAdmin()
    {
        _factory.Users.Setup(r => r.CountByTierAsync(SubscriptionTier.Free)).ReturnsAsync(10);
        _factory.Users.Setup(r => r.CountByTierAsync(SubscriptionTier.Pro)).ReturnsAsync(3);
        _factory.Users.Setup(r => r.CountByTierAsync(SubscriptionTier.Business)).ReturnsAsync(1);

        var reponse = await Admin.GetAsync(Dashboard);
        var tableau = await reponse.Content.ReadFromJsonAsync<AdminDashboardResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);

        // Le total est calculé par le service, pas lu : c'est l'agrégation qui est vérifiée ici.
        Assert.Equal(14, tableau!.TotalUsers);
        Assert.Equal(10, tableau.UsersByTier.Free);
    }

    // ── Santé du système ──────────────────────────────────────────

    [Fact]
    public async Task GetAdminSystemHealth_ShouldReturn200_WhenUserIsAdmin()
    {
        _factory.JobMonitoring.Setup(s => s.GetJobsStatusAsync()).ReturnsAsync([]);
        _factory.FoodItems.Setup(r => r.CountAsync()).ReturnsAsync(4242);

        var reponse = await Admin.GetAsync(Health);
        var sante = await reponse.Content.ReadFromJsonAsync<SystemHealthResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(4242, sante!.FoodItemsCount);

        // Aucun job n'a encore tourné : la date du dernier import doit rester absente,
        // et non prendre une valeur par défaut.
        Assert.Null(sante.LastImportAt);
    }

    [Fact]
    public async Task GetAdminSystemHealth_ShouldReturn403_WhenUserIsNotAdmin()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Health);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // ── Création de template ───────────────────────────

    [Fact]
    public async Task PostAdminDietPlanTemplates_ShouldReturn201_WhenRequestIsValid()
    {
        DietPlan? cree = null;
        _factory.DietPlans
            .Setup(r => r.AddAsync(It.IsAny<DietPlan>()))
            .Callback<DietPlan>(p => cree = p)
            .Returns(Task.CompletedTask);

        var reponse = await Admin.PostAsJsonAsync(Templates, RequeteCreation());

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);

        // Un template n'a pas de propriétaire : c'est ce qui le distingue d'un plan personnel.
        Assert.True(cree!.IsTemplate);
        Assert.Null(cree.UserId);
    }

    [Fact]
    public async Task PostAdminDietPlanTemplates_ShouldReturn422_WhenMacrosAreInvalid()
    {
        var requete = new CreateDietPlanRequest("Bancal", DietType.Balanced, Goal.Maintenance, 70f,
            new MacroDistributionDto(10, 10, 10));

        var reponse = await Admin.PostAsJsonAsync(Templates, requete);

        // Depuis NTR-135, un invariant de domaine violé donne 422 et non 500.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.AddAsync(It.IsAny<DietPlan>()), Times.Never);
    }

    // ── Modification de template ───────────────────────

    [Fact]
    public async Task PutAdminDietPlanTemplate_ShouldReturn200_WhenTemplateExists()
    {
        var template = Plan(template: true);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(template.Id)).ReturnsAsync(template);

        var reponse = await Admin.PutAsJsonAsync($"{Templates}/{template.Id}", RequeteMiseAJour());
        var misAJour = await reponse.Content.ReadFromJsonAsync<DietPlanResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Équilibre révisé", misAJour!.Name);
    }

    [Fact]
    public async Task PutAdminDietPlanTemplate_ShouldReturn404_WhenTemplateDoesNotExist()
    {
        var inconnu = Guid.NewGuid();
        _factory.DietPlans.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((DietPlan?)null);

        var reponse = await Admin.PutAsJsonAsync($"{Templates}/{inconnu}", RequeteMiseAJour());

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task PutAdminDietPlanTemplate_ShouldReturn404_WhenPlanIsPersonal()
    {
        var personnel = Plan(template: false);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(personnel.Id)).ReturnsAsync(personnel);

        var reponse = await Admin.PutAsJsonAsync($"{Templates}/{personnel.Id}", RequeteMiseAJour());

        // Le plan existe, mais n'est pas un template : la route admin doit l'ignorer plutôt que
        // de révéler son existence.
        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.UpdateAsync(It.IsAny<DietPlan>()), Times.Never);
    }

    // ── Suppression de template ────────────────────────

    [Fact]
    public async Task DeleteAdminDietPlanTemplate_ShouldReturn204_WhenTemplateExists()
    {
        var template = Plan(template: true);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(template.Id)).ReturnsAsync(template);

        var reponse = await Admin.DeleteAsync($"{Templates}/{template.Id}");

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.DeleteAsync(template.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteAdminDietPlanTemplate_ShouldReturn404_WhenTemplateDoesNotExist()
    {
        var inconnu = Guid.NewGuid();
        _factory.DietPlans.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((DietPlan?)null);

        var reponse = await Admin.DeleteAsync($"{Templates}/{inconnu}");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.DeleteAsync(inconnu), Times.Never);
    }

    // ── Accès anonyme ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RoutesAdmin_SansIdentite_Retournent401()
    {
        var reponse = await _factory.CreateAnonymousClient().GetAsync(Dashboard);

        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }
}
