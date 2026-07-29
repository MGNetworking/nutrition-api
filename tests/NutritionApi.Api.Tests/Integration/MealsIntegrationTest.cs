namespace NutritionApi.Api.Tests.Integration;

using Moq;
using NutritionApi.Api.Tests.Integration.Fixtures;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>MealsController</c> — NTR-111. Couvre IT-ML-01 à IT-ML-16 et la limite
/// de repas sauvegardés du palier d'abonnement.
/// </summary>
[Collection(ApiCollection.Name)]
public class MealsIntegrationTest
{
    private const string Endpoint = "/api/v1/meals";

    private readonly ApiFactory _factory;

    public MealsIntegrationTest(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetInvocations();
    }

    private static FoodItem Aliment(string nom = "Poulet")
        => new("3017620422003", nom, 165f, 31, 0, 4, []);

    /// <summary>Rend l'utilisateur courant résoluble et fixe son palier.</summary>
    private User GivenUtilisateurCourant(SubscriptionTier tier = SubscriptionTier.Free)
    {
        var utilisateur = _factory.CurrentUser;
        utilisateur.ChangeSubscriptionTier(tier);
        _factory.Users.Setup(r => r.GetByIdAsync(utilisateur.Id)).ReturnsAsync(utilisateur);
        return utilisateur;
    }

    /// <summary>Construit un repas complet — un repas sans aucun item est refusé par le domaine.</summary>
    /// <param name="items">Nombre d'aliments composant le repas — le domaine en exige au moins un.</param>
    private Meal RepasDe(Guid proprietaire, FoodItem aliment, string nom = "Déjeuner", bool sauvegarde = false, int items = 1)
        => new(proprietaire, nom, MealType.Lunch, null,
               [.. Enumerable.Range(0, items).Select(_ =>
                   new MealItem(Guid.NewGuid(), aliment.Id, 150f, new NutritionInfo(248f, 46, 0, 6)) { FoodItem = aliment })],
               DateTime.UtcNow, sauvegarde);

    private static CreateMealRequest RequeteCreation(Guid alimentId, bool sauvegarde = false)
        => new("Déjeuner", MealType.Lunch, DateTime.UtcNow, null, sauvegarde, [new MealItemRequest(alimentId, 150f)]);

    /// <summary>Arme le catalogue pour que les aliments demandés soient résolus.</summary>
    private void GivenCatalogue(params FoodItem[] aliments)
        => _factory.FoodItems
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync(aliments.ToList());

    // ── Création — IT-ML-01, IT-ML-02, IT-ML-03 ──────────────────────────────

    [Fact]
    public async Task IT_ML_01_CreationValide_Retourne201()
    {
        GivenUtilisateurCourant();
        var aliment = Aliment();
        GivenCatalogue(aliment);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Endpoint, RequeteCreation(aliment.Id));

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        _factory.Meals.Verify(r => r.AddAsync(It.IsAny<Meal>()), Times.Once);
    }

    [Fact]
    public async Task IT_ML_02_LimiteDeRepasSauvegardesAtteinte_Retourne403()
    {
        var utilisateur = GivenUtilisateurCourant(SubscriptionTier.Free);
        var aliment = Aliment();
        GivenCatalogue(aliment);
        _factory.Meals.Setup(r => r.CountSavedByUserIdAsync(utilisateur.Id)).ReturnsAsync(5);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Endpoint, RequeteCreation(aliment.Id, sauvegarde: true));

        // Free : 5 repas sauvegardés au maximum. La limite ne s'applique qu'aux repas sauvegardés.
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
        _factory.Meals.Verify(r => r.AddAsync(It.IsAny<Meal>()), Times.Never);
    }

    [Fact]
    public async Task IT_ML_02_UnRepasPonctuelNestPasSoumisALaLimite()
    {
        var utilisateur = GivenUtilisateurCourant(SubscriptionTier.Free);
        var aliment = Aliment();
        GivenCatalogue(aliment);
        _factory.Meals.Setup(r => r.CountSavedByUserIdAsync(utilisateur.Id)).ReturnsAsync(99);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Endpoint, RequeteCreation(aliment.Id, sauvegarde: false));

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_ML_03_AlimentReferenceInexistant_Retourne404()
    {
        GivenUtilisateurCourant();
        GivenCatalogue();   // le catalogue ne renvoie rien

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Endpoint, RequeteCreation(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task Creation_AvecUneQuantiteNegative_Retourne422()
    {
        GivenUtilisateurCourant();
        var aliment = Aliment();
        GivenCatalogue(aliment);
        var requete = new CreateMealRequest("Déjeuner", MealType.Lunch, DateTime.UtcNow, null, false,
            [new MealItemRequest(aliment.Id, -10f)]);

        var reponse = await _factory.CreateAuthenticatedClient().PostAsJsonAsync(Endpoint, requete);

        // Invariant de MealItem — 422 depuis NTR-135.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
    }

    // ── Consultation — IT-ML-04 à IT-ML-08 ───────────────────────────────────

    [Fact]
    public async Task IT_ML_04_ListeSansFiltre_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.Meals
            .Setup(r => r.GetByUserIdAsync(utilisateur.Id, null, null))
            .ReturnsAsync([RepasDe(utilisateur.Id, Aliment())]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Endpoint);
        var repas = await reponse.Content.ReadFromJsonAsync<List<MealResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Single(repas!);
    }

    [Fact]
    public async Task IT_ML_05_FiltreSurLesRepasSauvegardes_EstTransmisAuDepot()
    {
        var utilisateur = GivenUtilisateurCourant();
        bool? filtreRecu = null;
        _factory.Meals
            .Setup(r => r.GetByUserIdAsync(utilisateur.Id, It.IsAny<DateOnly?>(), It.IsAny<bool?>()))
            .Callback<Guid, DateOnly?, bool?>((_, _, saved) => filtreRecu = saved)
            .ReturnsAsync([]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}?saved=true");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.True(filtreRecu);
    }

    [Fact]
    public async Task IT_ML_06_FiltreParDate_EstTransmisAuDepot()
    {
        var utilisateur = GivenUtilisateurCourant();
        DateOnly? dateRecue = null;
        _factory.Meals
            .Setup(r => r.GetByUserIdAsync(utilisateur.Id, It.IsAny<DateOnly?>(), It.IsAny<bool?>()))
            .Callback<Guid, DateOnly?, bool?>((_, date, _) => dateRecue = date)
            .ReturnsAsync([]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}?date=2024-01-15");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(new DateOnly(2024, 1, 15), dateRecue);
    }

    [Fact]
    public async Task IT_ML_07_RepasExistantEtProprietaire_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        var repas = RepasDe(utilisateur.Id, Aliment());
        _factory.Meals.Setup(r => r.GetByIdAsync(repas.Id)).ReturnsAsync(repas);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/{repas.Id}");

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_ML_08_RepasInexistant_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.Meals.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((Meal?)null);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/{inconnu}");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task ConsultationDuRepasDunAutre_Retourne403()
    {
        GivenUtilisateurCourant();
        var repasDunAutre = RepasDe(Guid.NewGuid(), Aliment());
        _factory.Meals.Setup(r => r.GetByIdAsync(repasDunAutre.Id)).ReturnsAsync(repasDunAutre);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync($"{Endpoint}/{repasDunAutre.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }

    // ── Modification et suppression — IT-ML-09 à IT-ML-12 ────────────────────

    [Fact]
    public async Task IT_ML_09_ModificationDunRepasExistant_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        var repas = RepasDe(utilisateur.Id, Aliment());
        _factory.Meals.Setup(r => r.GetByIdAsync(repas.Id)).ReturnsAsync(repas);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PatchAsJsonAsync($"{Endpoint}/{repas.Id}", new UpdateMealRequest("Dîner", null, null, null, null));
        var misAJour = await reponse.Content.ReadFromJsonAsync<MealResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal("Dîner", misAJour!.Name);
    }

    [Fact]
    public async Task IT_ML_10_ModificationDunRepasInexistant_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.Meals.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((Meal?)null);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PatchAsJsonAsync($"{Endpoint}/{inconnu}", new UpdateMealRequest("Dîner", null, null, null, null));

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_ML_11_SuppressionDunRepasExistant_Retourne204()
    {
        var utilisateur = GivenUtilisateurCourant();
        var repas = RepasDe(utilisateur.Id, Aliment());
        _factory.Meals.Setup(r => r.GetByIdAsync(repas.Id)).ReturnsAsync(repas);

        var reponse = await _factory.CreateAuthenticatedClient().DeleteAsync($"{Endpoint}/{repas.Id}");

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        _factory.Meals.Verify(r => r.DeleteAsync(repas.Id), Times.Once);
    }

    [Fact]
    public async Task IT_ML_12_SuppressionDunRepasInexistant_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.Meals.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((Meal?)null);

        var reponse = await _factory.CreateAuthenticatedClient().DeleteAsync($"{Endpoint}/{inconnu}");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    // ── Items du repas — IT-ML-13 à IT-ML-16 ─────────────────────────────────

    [Fact]
    public async Task IT_ML_13_AjoutDunItem_Retourne201()
    {
        var utilisateur = GivenUtilisateurCourant();
        var aliment = Aliment();
        var repas = RepasDe(utilisateur.Id, aliment);
        var nouvelAliment = Aliment("Riz");
        _factory.Meals.Setup(r => r.GetByIdAsync(repas.Id)).ReturnsAsync(repas);
        _factory.FoodItems.Setup(r => r.GetByIdAsync(nouvelAliment.Id)).ReturnsAsync(nouvelAliment);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync($"{Endpoint}/{repas.Id}/items", new AddMealItemRequest(nouvelAliment.Id, 200f));
        var misAJour = await reponse.Content.ReadFromJsonAsync<MealResponse>();

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        Assert.Equal(2, misAJour!.Items.Count);
    }

    [Fact]
    public async Task IT_ML_14_AjoutDunItemSurUnRepasInexistant_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.Meals.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((Meal?)null);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync($"{Endpoint}/{inconnu}/items", new AddMealItemRequest(Guid.NewGuid(), 200f));

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_ML_15_SuppressionDunItem_Retourne204()
    {
        var utilisateur = GivenUtilisateurCourant();
        var repas = RepasDe(utilisateur.Id, Aliment(), items: 2);
        var itemId = repas.MealItems.First().Id;
        _factory.Meals.Setup(r => r.GetByIdAsync(repas.Id)).ReturnsAsync(repas);

        var reponse = await _factory.CreateAuthenticatedClient()
            .DeleteAsync($"{Endpoint}/{repas.Id}/items/{itemId}");

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        Assert.Single(repas.MealItems);
    }

    [Fact]
    public async Task SuppressionDuDernierItem_Retourne422()
    {
        var utilisateur = GivenUtilisateurCourant();
        var repas = RepasDe(utilisateur.Id, Aliment(), items: 1);
        var itemId = repas.MealItems.First().Id;
        _factory.Meals.Setup(r => r.GetByIdAsync(repas.Id)).ReturnsAsync(repas);

        var reponse = await _factory.CreateAuthenticatedClient()
            .DeleteAsync($"{Endpoint}/{repas.Id}/items/{itemId}");

        // Le domaine interdit de vider un repas — il faut supprimer le repas lui-même. Sans la
        // traduction dans MealService, l'InvalidOperationException donnait un 500.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
        Assert.Single(repas.MealItems);
    }

    [Fact]
    public async Task IT_ML_16_SuppressionDunItemInexistant_Retourne404()
    {
        var utilisateur = GivenUtilisateurCourant();
        var repas = RepasDe(utilisateur.Id, Aliment());
        _factory.Meals.Setup(r => r.GetByIdAsync(repas.Id)).ReturnsAsync(repas);

        var reponse = await _factory.CreateAuthenticatedClient()
            .DeleteAsync($"{Endpoint}/{repas.Id}/items/{Guid.NewGuid()}");

        // 404 et non 422 : c'est la correction apportée par NTR-135, lot B. Le domaine levait une
        // ArgumentException, MealService vérifie désormais l'existence en amont.
        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }
}
