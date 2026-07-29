namespace NutritionApi.Api.Tests.Integration;

using Moq;
using NutritionApi.Api.Tests.Integration.Fixtures;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>DietsController</c> — NTR-109. Couvre IT-DT-01 à IT-DT-12.
/// </summary>
[Collection(ApiCollection.Name)]
public class DietsIntegrationTest
{
    private const string Endpoint = "/api/v1/diets";

    private readonly ApiFactory _factory;

    public DietsIntegrationTest(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetInvocations();
    }

    private static MacroDistribution Macros => new(30, 40, 30);

    private Diet DieteDe(Guid proprietaire, string nom = "Sèche")
        => new(proprietaire, nom, DietType.HighProtein, Goal.WeightLoss, 72f, 1800, Macros);

    private DietPlan PlanDe(Guid? proprietaire, bool template = false)
        => new(proprietaire, "Sèche", template, DietType.HighProtein, Goal.WeightLoss, 72f, Macros);

    /// <summary>Rend l'utilisateur courant résoluble et fixe son palier.</summary>
    private User GivenUtilisateurCourant(SubscriptionTier tier = SubscriptionTier.Free)
    {
        var utilisateur = _factory.CurrentUser;
        utilisateur.ChangeSubscriptionTier(tier);
        _factory.Users.Setup(r => r.GetByIdAsync(utilisateur.Id)).ReturnsAsync(utilisateur);
        return utilisateur;
    }

    /// <summary>Arme les prérequis d'un lancement réussi : aucune diète active, une pesée existante.</summary>
    private void GivenLancementPossible(Guid userId)
    {
        _factory.Diets.Setup(r => r.GetActiveByUserIdAsync(userId)).ReturnsAsync((Diet?)null);
        _factory.WeightEntries
            .Setup(r => r.GetByUserIdAsync(userId))
            .ReturnsAsync([new WeightEntry(userId, 78f, DateOnly.FromDateTime(DateTime.UtcNow))]);
    }

    // ── Diète active — IT-DT-01, IT-DT-02 ─────────────────────────────────────

    [Fact]
    public async Task IT_DT_01_DieteActiveExistante_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.Diets.Setup(r => r.GetActiveByUserIdAsync(utilisateur.Id)).ReturnsAsync(DieteDe(utilisateur.Id));

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/active");
        var diete = await reponse.Content.ReadFromJsonAsync<DietResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Sèche", diete!.Name);
    }

    [Fact]
    public async Task IT_DT_02_AucuneDieteActive_Retourne404()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.Diets.Setup(r => r.GetActiveByUserIdAsync(utilisateur.Id)).ReturnsAsync((Diet?)null);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/active");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    // ── Historique et consultation — IT-DT-03, IT-DT-04, IT-DT-05 ─────────────

    [Fact]
    public async Task IT_DT_03_HistoriqueNonVide_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.Diets
            .Setup(r => r.GetByUserIdAsync(utilisateur.Id))
            .ReturnsAsync([DieteDe(utilisateur.Id, "Ancienne sèche")]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Endpoint);
        var historique = await reponse.Content.ReadFromJsonAsync<List<DietResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Ancienne sèche", Assert.Single(historique!).Name);
    }

    [Fact]
    public async Task IT_DT_04_DieteExistanteEtProprietaire_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        var diete = DieteDe(utilisateur.Id);
        _factory.Diets.Setup(r => r.GetByIdAsync(diete.Id)).ReturnsAsync(diete);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/{diete.Id}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_DT_05_DieteInexistante_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnue = Guid.NewGuid();
        _factory.Diets.Setup(r => r.GetByIdAsync(inconnue)).ReturnsAsync((Diet?)null);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/{inconnue}");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task ConsultationDeLaDieteDunAutre_Retourne403()
    {
        GivenUtilisateurCourant();
        var dieteDunAutre = DieteDe(Guid.NewGuid());
        _factory.Diets.Setup(r => r.GetByIdAsync(dieteDunAutre.Id)).ReturnsAsync(dieteDunAutre);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/{dieteDunAutre.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // ── Archivage — IT-DT-06, IT-DT-07, IT-DT-08 ─────────────────────────────

    [Fact]
    public async Task IT_DT_06_ArchivageDuneDieteActive_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        var diete = DieteDe(utilisateur.Id);
        _factory.Diets.Setup(r => r.GetByIdAsync(diete.Id)).ReturnsAsync(diete);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{diete.Id}/archive", null);
        var archivee = await reponse.Content.ReadFromJsonAsync<DietResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.NotEqual(DietStatus.Active, archivee!.Status);
    }

    [Fact]
    public async Task IT_DT_07_ArchivageDuneDieteDejaArchivee_Retourne422()
    {
        var utilisateur = GivenUtilisateurCourant();
        var diete = DieteDe(utilisateur.Id);
        diete.ChangeDietStatus(DietStatus.Archived);
        _factory.Diets.Setup(r => r.GetByIdAsync(diete.Id)).ReturnsAsync(diete);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{diete.Id}/archive", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_DT_08_ArchivageDuneDieteInexistante_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnue = Guid.NewGuid();
        _factory.Diets.Setup(r => r.GetByIdAsync(inconnue)).ReturnsAsync((Diet?)null);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{inconnue}/archive", null);

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    // ── Lancement — IT-DT-09 à IT-DT-12 ──────────────────────────────────────

    [Fact]
    public async Task IT_DT_09_LancementValide_Retourne201()
    {
        var utilisateur = GivenUtilisateurCourant();
        var plan = PlanDe(utilisateur.Id);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        GivenLancementPossible(utilisateur.Id);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{plan.Id}/launch", null);

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        _factory.Diets.Verify(r => r.AddAsync(It.IsAny<Diet>()), Times.Once);
    }

    [Fact]
    public async Task IT_DT_10_LancementAvecUneDieteDejaActive_Retourne409()
    {
        var utilisateur = GivenUtilisateurCourant();
        var plan = PlanDe(utilisateur.Id);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _factory.Diets.Setup(r => r.GetActiveByUserIdAsync(utilisateur.Id)).ReturnsAsync(DieteDe(utilisateur.Id));

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{plan.Id}/launch", null);

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        _factory.Diets.Verify(r => r.AddAsync(It.IsAny<Diet>()), Times.Never);
    }

    [Fact]
    public async Task IT_DT_11_LancementDunPlanInexistant_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.DietPlans.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((DietPlan?)null);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{inconnu}/launch", null);

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_DT_12_LancementSansAucunePesee_Retourne422()
    {
        var utilisateur = GivenUtilisateurCourant();
        var plan = PlanDe(utilisateur.Id);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(plan.Id)).ReturnsAsync(plan);
        _factory.Diets.Setup(r => r.GetActiveByUserIdAsync(utilisateur.Id)).ReturnsAsync((Diet?)null);
        _factory.WeightEntries.Setup(r => r.GetByUserIdAsync(utilisateur.Id)).ReturnsAsync([]);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{plan.Id}/launch", null);

        // L'objectif calorique se calcule à partir du dernier poids connu : sans pesée, la demande
        // est recevable mais inexploitable.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
    }

    [Fact]
    public async Task LancementDunTemplateEnPalierFree_Retourne403()
    {
        var utilisateur = GivenUtilisateurCourant(SubscriptionTier.Free);
        var template = PlanDe(null, template: true);
        _factory.DietPlans.Setup(r => r.GetByIdAsync(template.Id)).ReturnsAsync(template);
        GivenLancementPossible(utilisateur.Id);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsync($"{Endpoint}/{template.Id}/launch", null);

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }
}
