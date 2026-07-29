namespace NutritionApi.Api.Tests.Integration;

using Moq;
using NutritionApi.Api.Tests.Integration.Fixtures;
using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>AdminController</c> — NTR-106. Couvre IT-ADM-01 à IT-ADM-09.
/// </summary>
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

    // ── Tableau de bord — IT-ADM-01, IT-ADM-02 ────────────────────────────────

    [Fact]
    public async Task IT_ADM_01_SansRoleAdmin_Retourne403()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Dashboard);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_ADM_02_AvecRoleAdmin_Retourne200AvecLesIndicateurs()
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

    // ── Santé du système — IT-ADM-03 ──────────────────────────────────────────

    [Fact]
    public async Task IT_ADM_03_SanteDuSysteme_Retourne200()
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
    public async Task IT_ADM_03_SanteDuSysteme_SansRoleAdmin_Retourne403()
    {
        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Health);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // ── Création de template — IT-ADM-04, IT-ADM-05 ───────────────────────────

    [Fact]
    public async Task IT_ADM_04_CreationValide_Retourne201()
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
    public async Task IT_ADM_05_MacrosInvalides_Retourne422()
    {
        var requete = new CreateDietPlanRequest("Bancal", DietType.Balanced, Goal.Maintenance, 70f,
            new MacroDistributionDto(10, 10, 10));

        var reponse = await Admin.PostAsJsonAsync(Templates, requete);

        // Depuis NTR-135, un invariant de domaine violé donne 422 et non 500.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.AddAsync(It.IsAny<DietPlan>()), Times.Never);
    }

    // ── Modification de template — IT-ADM-06, IT-ADM-07 ───────────────────────

    [Fact]
    public async Task IT_ADM_06_ModificationDunTemplateExistant_Retourne200()
    {
        var template = Plan(template: true);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(template.Id)).ReturnsAsync(template);

        var reponse = await Admin.PutAsJsonAsync($"{Templates}/{template.Id}", RequeteMiseAJour());
        var misAJour = await reponse.Content.ReadFromJsonAsync<DietPlanResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Équilibre révisé", misAJour!.Name);
    }

    [Fact]
    public async Task IT_ADM_07_ModificationDunTemplateInexistant_Retourne404()
    {
        var inconnu = Guid.NewGuid();
        _factory.DietPlans.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((DietPlan?)null);

        var reponse = await Admin.PutAsJsonAsync($"{Templates}/{inconnu}", RequeteMiseAJour());

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_ADM_07_UnPlanPersonnelNestPasAtteignableParLaRouteAdmin()
    {
        var personnel = Plan(template: false);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(personnel.Id)).ReturnsAsync(personnel);

        var reponse = await Admin.PutAsJsonAsync($"{Templates}/{personnel.Id}", RequeteMiseAJour());

        // Le plan existe, mais n'est pas un template : la route admin doit l'ignorer plutôt que
        // de révéler son existence.
        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.UpdateAsync(It.IsAny<DietPlan>()), Times.Never);
    }

    // ── Suppression de template — IT-ADM-08, IT-ADM-09 ────────────────────────

    [Fact]
    public async Task IT_ADM_08_SuppressionDunTemplateExistant_Retourne204()
    {
        var template = Plan(template: true);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(template.Id)).ReturnsAsync(template);

        var reponse = await Admin.DeleteAsync($"{Templates}/{template.Id}");

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.DeleteAsync(template.Id), Times.Once);
    }

    [Fact]
    public async Task IT_ADM_09_SuppressionDunTemplateInexistant_Retourne404()
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
