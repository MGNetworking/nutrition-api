namespace NutritionApi.Api.Tests.Level2;

using Moq;
using NutritionApi.Api.Tests.Level2.Fixtures;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>DietPlansController</c> — NTR-108. Couvre la lecture, la création, la modification, la suppression et les templates,
/// la limite de plans et l'accès aux templates selon le palier d'abonnement.
/// </summary>
/// <remarks>
/// Ce qui est réel ici : routage, liaison de modèle, middlewares, autorisation, sérialisation,
/// <c>DietPlansService</c> et <c>SubscriptionGuard</c>. Seules les frontières de persistance sont
/// des doublures — voir <c>qualite/tests-niveau-2.md</c>.
/// </remarks>
[Trait("Level", "2")]
[Collection(ApiCollection.Name)]
public class DietPlansIntegrationTest
{
    private const string Endpoint = "/api/v1/diet-plans";

    private readonly ApiFactory _factory;

    public DietPlansIntegrationTest(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetInvocations();
    }

    private static MacroDistributionDto MacrosValides => new(30, 40, 30);

    private static CreateDietPlanRequest RequeteCreation(string nom = "Sèche")
        => new(nom, DietType.HighProtein, Goal.WeightLoss, 72f, MacrosValides);

    private static UpdateDietPlanRequest RequeteMiseAJour(string nom = "Sèche révisée")
        => new(nom, DietType.HighProtein, Goal.WeightLoss, 70f, MacrosValides);

    private DietPlan PlanDe(Guid proprietaire, string nom = "Sèche")
        => new(proprietaire, nom, false, DietType.HighProtein, Goal.WeightLoss, 72f,
               new MacroDistribution(30, 40, 30));

    /// <summary>Rend l'utilisateur courant résoluble par le service, avec le palier demandé.</summary>
    private User GivenUtilisateurCourant(SubscriptionTier tier = SubscriptionTier.Free)
    {
        var utilisateur = _factory.CurrentUser;
        utilisateur.ChangeSubscriptionTier(tier);
        _factory.Users.Setup(r => r.GetByIdAsync(utilisateur.Id)).ReturnsAsync(utilisateur);
        return utilisateur;
    }

    private void GivenNombreDePlans(int nombre)
        => _factory.DietPlans.Setup(r => r.CountByUserIdAsync(It.IsAny<Guid>())).ReturnsAsync(nombre);

    // ── Lecture ────────────────────────────────────────────────────

    [Fact]
    public async Task GetDietPlans_ShouldReturn200WithOwnedPlans_WhenUserHasPlans()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.DietPlans
            .Setup(r => r.GetByUserIdAsync(utilisateur.Id))
            .ReturnsAsync([PlanDe(utilisateur.Id, "Prise de masse")]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Endpoint);
        var plans = await reponse.Content.ReadFromJsonAsync<List<DietPlanResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Prise de masse", Assert.Single(plans!).Name);
    }

    // ── Création et limite de palier ─────────────────────

    [Fact]
    public async Task PostDietPlans_ShouldReturn201_WhenRequestIsValid()
    {
        GivenUtilisateurCourant();
        GivenNombreDePlans(0);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Endpoint, RequeteCreation());

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.AddAsync(It.IsAny<DietPlan>()), Times.Once);
        _factory.UnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task PostDietPlans_ShouldReject_WhenMacrosDoNotSumTo100()
    {
        GivenUtilisateurCourant();
        GivenNombreDePlans(0);

        var requete = new CreateDietPlanRequest("Sèche", DietType.HighProtein, Goal.WeightLoss, 72f,
            new MacroDistributionDto(10, 10, 10));

        var reponse = await _factory.CreateAuthenticatedClient().PostAsJsonAsync(Endpoint, requete);

        // L'invariant est porté par le Value Object MacroDistribution : la répartition doit totaliser 100 %.
        Assert.NotEqual(HttpStatusCode.Created, reponse.StatusCode);
        Assert.True(
            reponse.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Statut inattendu : {(int)reponse.StatusCode}");
    }

    [Fact]
    public async Task Creation_LimiteDuPalierAtteinte_Retourne403()
    {
        GivenUtilisateurCourant(SubscriptionTier.Free);
        GivenNombreDePlans(2);   // Free : 2 plans au maximum

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Endpoint, RequeteCreation());

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.AddAsync(It.IsAny<DietPlan>()), Times.Never);
    }

    [Fact]
    public async Task Creation_PalierProSousLaLimite_Retourne201()
    {
        GivenUtilisateurCourant(SubscriptionTier.Pro);
        GivenNombreDePlans(2);   // au-delà de la limite Free, mais Pro en autorise 20

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Endpoint, RequeteCreation());

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
    }

    [Fact]
    public async Task Creation_ChampNomAbsent_Retourne400()
    {
        GivenUtilisateurCourant();
        GivenNombreDePlans(0);

        // JSON syntaxiquement valide, mais le nom — non nullable — est absent.
        var corps = JsonContent.Create(new { dietType = 1, goal = 1, targetWeight = 72f, macroDistribution = new { proteinPct = 30, carbPct = 40, fatPct = 30 } });

        var reponse = await _factory.CreateAuthenticatedClient().PostAsync(Endpoint, corps);

        Assert.Equal(HttpStatusCode.BadRequest, reponse.StatusCode);
    }

    // ── Modification ───────────────────────────

    [Fact]
    public async Task PutDietPlan_ShouldReturn200_WhenPlanBelongsToUser()
    {
        var utilisateur = GivenUtilisateurCourant();
        var plan = PlanDe(utilisateur.Id);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PutAsJsonAsync($"{Endpoint}/{plan.Id}", RequeteMiseAJour("Sèche révisée"));
        var misAJour = await reponse.Content.ReadFromJsonAsync<DietPlanResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Sèche révisée", misAJour!.Name);
    }

    [Fact]
    public async Task PutDietPlan_ShouldReturn403_WhenPlanBelongsToAnotherUser()
    {
        GivenUtilisateurCourant();
        var planDunAutre = PlanDe(Guid.NewGuid());
        _factory.DietPlans.Setup(r => r.GetByIdAsync(planDunAutre.Id)).ReturnsAsync(planDunAutre);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PutAsJsonAsync($"{Endpoint}/{planDunAutre.Id}", RequeteMiseAJour());

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.UpdateAsync(It.IsAny<DietPlan>()), Times.Never);
    }

    [Fact]
    public async Task PutDietPlan_ShouldReturn404_WhenPlanDoesNotExist()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.DietPlans.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((DietPlan?)null);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PutAsJsonAsync($"{Endpoint}/{inconnu}", RequeteMiseAJour());

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    // ── Suppression ──────────────────────────────────────

    [Fact]
    public async Task DeleteDietPlan_ShouldReturn204_WhenPlanBelongsToUser()
    {
        var utilisateur = GivenUtilisateurCourant();
        var plan = PlanDe(utilisateur.Id);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);

        var reponse = await _factory.CreateAuthenticatedClient().DeleteAsync($"{Endpoint}/{plan.Id}");

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        _factory.DietPlans.Verify(r => r.DeleteAsync(plan.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteDietPlan_ShouldReturn404_WhenPlanDoesNotExist()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.DietPlans.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((DietPlan?)null);

        var reponse = await _factory.CreateAuthenticatedClient().DeleteAsync($"{Endpoint}/{inconnu}");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    // ── Templates ────────────────────────────────────────

    [Fact]
    public async Task GetDietPlanTemplates_ShouldReturn403_WhenTierIsFree()
    {
        GivenUtilisateurCourant(SubscriptionTier.Free);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/templates");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    [Fact]
    public async Task GetDietPlanTemplates_ShouldReturn200_WhenTierIsPro()
    {
        GivenUtilisateurCourant(SubscriptionTier.Pro);
        _factory.DietPlans
            .Setup(r => r.GetTemplatesAsync())
            .ReturnsAsync([PlanDe(Guid.NewGuid(), "Équilibre méditerranéen")]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/templates");
        var templates = await reponse.Content.ReadFromJsonAsync<List<DietPlanResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Équilibre méditerranéen", Assert.Single(templates!).Name);
    }
}
