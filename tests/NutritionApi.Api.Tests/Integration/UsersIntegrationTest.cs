namespace NutritionApi.Api.Tests.Integration;

using Moq;
using NutritionApi.Api.Tests.Integration.Fixtures;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

/// <summary>
/// Tests de niveau 2 de <c>UsersController</c> — NTR-107. Couvre IT-USR-01 à IT-USR-16 et la
/// limite d'aliments favoris du palier d'abonnement.
/// </summary>
[Collection(ApiCollection.Name)]
public class UsersIntegrationTest
{
    private const string Me = "/api/v1/users/me";
    private const string Pesees = "/api/v1/users/me/weight-entries";
    private const string Favoris = "/api/v1/users/me/saved-food-items";

    private const string SubInconnu = "00000000-0000-0000-0000-0000000000ff";

    private readonly ApiFactory _factory;

    public UsersIntegrationTest(ApiFactory factory)
    {
        _factory = factory;
        _factory.ResetInvocations();
    }

    private static CreateUserProfileRequest RequeteCreation()
        => new(new DateOnly(1992, 3, 15), Gender.Male, ActivityLevel.Sedentary, 180f, [], [], 78.5f);

    private static UpdateUserProfileRequest RequeteMiseAJour(float taille = 181f)
        => new(new DateOnly(1992, 3, 15), Gender.Male, ActivityLevel.ModeratelyActive, taille, [], []);

    /// <summary>Rend l'utilisateur courant résoluble par identifiant interne, avec le palier voulu.</summary>
    private User GivenUtilisateurCourant(SubscriptionTier tier = SubscriptionTier.Free)
    {
        var utilisateur = _factory.CurrentUser;
        utilisateur.ChangeSubscriptionTier(tier);
        _factory.Users.Setup(r => r.GetByIdAsync(utilisateur.Id)).ReturnsAsync(utilisateur);
        return utilisateur;
    }

    private static FoodItem Aliment(string nom = "Poulet")
        => new("3017620422003", nom, 165f, 31, 0, 4, []);

    // ── Profil — IT-USR-01 à IT-USR-06 ────────────────────────────────────────

    [Fact]
    public async Task IT_USR_01_CreationDunNouveauProfil_Retourne201()
    {
        // Aucun profil pour ce sub : c'est exactement la situation d'un nouvel utilisateur.
        _factory.Users.Setup(r => r.GetByKeycloakIdAsync(SubInconnu)).ReturnsAsync((User?)null);

        var reponse = await _factory.CreateAuthenticatedClient(subject: SubInconnu)
            .PostAsJsonAsync(Me, RequeteCreation());

        // L'attribut [AllowWithoutProfile] dispense cette action de l'exigence de profil.
        // Sans lui, UserResolutionMiddleware couperait la chaîne et la création serait
        // inatteignable — il faudrait déjà posséder un profil pour en créer un.
        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        _factory.Users.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
        _factory.WeightEntries.Verify(r => r.AddAsync(It.IsAny<WeightEntry>()), Times.Once);
    }

    [Fact]
    public async Task IT_USR_02_ProfilDejaExistant_Retourne409()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.Users.Setup(r => r.GetByKeycloakIdAsync(ApiFactory.DefaultSubject)).ReturnsAsync(utilisateur);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Me, RequeteCreation());

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
    }

    [Fact]
    public async Task LesAutresRoutes_SansProfil_Retournent401()
    {
        _factory.Users.Setup(r => r.GetByKeycloakIdAsync(SubInconnu)).ReturnsAsync((User?)null);

        var reponse = await _factory.CreateAuthenticatedClient(subject: SubInconnu).GetAsync(Me);

        // La dispense ne vaut que pour la création : partout ailleurs, un jeton valide sans profil
        // reste refusé.
        Assert.Equal(HttpStatusCode.Unauthorized, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_USR_03_LectureDuProfil_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Me);
        var profil = await reponse.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(utilisateur.Id, profil!.Id);
    }

    [Fact]
    public async Task IT_USR_05_MiseAJourDuProfil_Retourne200()
    {
        GivenUtilisateurCourant();

        var reponse = await _factory.CreateAuthenticatedClient().PutAsJsonAsync(Me, RequeteMiseAJour(181f));
        var profil = await reponse.Content.ReadFromJsonAsync<UserProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Equal(181f, profil!.Height);
        _factory.UnitOfWork.Verify(u => u.SaveChangesAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task MiseAJourAvecUneDateDeNaissanceFuture_Retourne422()
    {
        GivenUtilisateurCourant();
        var requete = new UpdateUserProfileRequest(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)),
            Gender.Male, ActivityLevel.ModeratelyActive, 180f, [], []);

        var reponse = await _factory.CreateAuthenticatedClient().PutAsJsonAsync(Me, requete);

        // Invariant du domaine — 422 depuis NTR-135, 500 auparavant.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
    }

    [Fact]
    public async Task MiseAJourAvecUneTailleNegative_Retourne422()
    {
        GivenUtilisateurCourant();

        var reponse = await _factory.CreateAuthenticatedClient().PutAsJsonAsync(Me, RequeteMiseAJour(-5f));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, reponse.StatusCode);
    }

    // ── Pesées — IT-USR-07 à IT-USR-11 ────────────────────────────────────────

    [Fact]
    public async Task IT_USR_07_AjoutDunePesee_Retourne201()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.WeightEntries
            .Setup(r => r.GetByUserIdAndDateAsync(utilisateur.Id, It.IsAny<DateOnly>()))
            .ReturnsAsync((WeightEntry?)null);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Pesees, new AddWeightEntryRequest(77.4f, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
        _factory.WeightEntries.Verify(r => r.AddAsync(It.IsAny<WeightEntry>()), Times.Once);
    }

    [Fact]
    public async Task IT_USR_08_PeseeDejaEnregistreeALaMemeDate_Retourne409()
    {
        var utilisateur = GivenUtilisateurCourant();
        var date = DateOnly.FromDateTime(DateTime.UtcNow);
        _factory.WeightEntries
            .Setup(r => r.GetByUserIdAndDateAsync(utilisateur.Id, date))
            .ReturnsAsync(new WeightEntry(utilisateur.Id, 78f, date));

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Pesees, new AddWeightEntryRequest(77.4f, date));

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
        _factory.WeightEntries.Verify(r => r.AddAsync(It.IsAny<WeightEntry>()), Times.Never);
    }

    [Fact]
    public async Task IT_USR_09_HistoriqueDesPesees_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        _factory.WeightEntries
            .Setup(r => r.GetByUserIdAsync(utilisateur.Id))
            .ReturnsAsync([new WeightEntry(utilisateur.Id, 78f, DateOnly.FromDateTime(DateTime.UtcNow))]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Pesees);
        var historique = await reponse.Content.ReadFromJsonAsync<List<WeightEntryResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Single(historique!);
    }

    [Fact]
    public async Task IT_USR_10_MiseAJourDunePesee_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        var pesee = new WeightEntry(utilisateur.Id, 78f, DateOnly.FromDateTime(DateTime.UtcNow));
        _factory.WeightEntries.Setup(r => r.GetByIdAsync(pesee.Id)).ReturnsAsync(pesee);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PutAsJsonAsync($"{Pesees}/{pesee.Id}", new UpdateWeightEntryRequest(76.2f, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_USR_11_MiseAJourDunePeseeInexistante_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnue = Guid.NewGuid();
        _factory.WeightEntries.Setup(r => r.GetByIdAsync(inconnue)).ReturnsAsync((WeightEntry?)null);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PutAsJsonAsync($"{Pesees}/{inconnue}", new UpdateWeightEntryRequest(76.2f, DateOnly.FromDateTime(DateTime.UtcNow)));

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task MiseAJourDeLaPeseeDunAutreUtilisateur_Retourne404()
    {
        GivenUtilisateurCourant();
        var peseeDunAutre = new WeightEntry(Guid.NewGuid(), 90f, DateOnly.FromDateTime(DateTime.UtcNow));
        _factory.WeightEntries.Setup(r => r.GetByIdAsync(peseeDunAutre.Id)).ReturnsAsync(peseeDunAutre);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PutAsJsonAsync($"{Pesees}/{peseeDunAutre.Id}", new UpdateWeightEntryRequest(76.2f, DateOnly.FromDateTime(DateTime.UtcNow)));

        // 404 et non 403 : révéler l'existence de la pesée d'autrui serait une fuite d'information.
        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    // ── Aliments favoris — IT-USR-12 à IT-USR-16 ──────────────────────────────

    [Fact]
    public async Task IT_USR_12_ListeDesFavoris_Retourne200()
    {
        var utilisateur = GivenUtilisateurCourant();
        var aliment = Aliment();
        _factory.SavedFoodItems
            .Setup(r => r.GetByUserIdAsync(utilisateur.Id))
            .ReturnsAsync([new SavedFoodItem(utilisateur.Id, aliment.Id)]);
        _factory.FoodItems
            .Setup(r => r.GetByIdsAsync(It.IsAny<List<Guid>>()))
            .ReturnsAsync([aliment]);

        var reponse = await _factory.CreateAuthenticatedClient().GetAsync(Favoris);
        var favoris = await reponse.Content.ReadFromJsonAsync<List<SavedFoodItemResponse>>();

        Assert.Equal(HttpStatusCode.OK, reponse.StatusCode);
        Assert.Single(favoris!);
    }

    [Fact]
    public async Task IT_USR_13_AjoutDunFavori_Retourne201()
    {
        var utilisateur = GivenUtilisateurCourant();
        var aliment = Aliment();
        _factory.SavedFoodItems
            .Setup(r => r.GetByUserIdAndFoodItemIdAsync(utilisateur.Id, aliment.Id))
            .ReturnsAsync((SavedFoodItem?)null);
        _factory.SavedFoodItems.Setup(r => r.CountByUserIdAsync(utilisateur.Id)).ReturnsAsync(0);
        _factory.FoodItems.Setup(r => r.GetByIdAsync(aliment.Id)).ReturnsAsync(aliment);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Favoris, new SaveFoodItemRequest(aliment.Id));

        Assert.Equal(HttpStatusCode.Created, reponse.StatusCode);
    }

    [Fact]
    public async Task IT_USR_14_AlimentDejaEnFavori_Retourne409()
    {
        var utilisateur = GivenUtilisateurCourant();
        var aliment = Aliment();
        _factory.SavedFoodItems
            .Setup(r => r.GetByUserIdAndFoodItemIdAsync(utilisateur.Id, aliment.Id))
            .ReturnsAsync(new SavedFoodItem(utilisateur.Id, aliment.Id));

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Favoris, new SaveFoodItemRequest(aliment.Id));

        Assert.Equal(HttpStatusCode.Conflict, reponse.StatusCode);
    }

    [Fact]
    public async Task AjoutDunFavori_LimiteDuPalierAtteinte_Retourne403()
    {
        var utilisateur = GivenUtilisateurCourant(SubscriptionTier.Free);
        var aliment = Aliment();
        _factory.SavedFoodItems
            .Setup(r => r.GetByUserIdAndFoodItemIdAsync(utilisateur.Id, aliment.Id))
            .ReturnsAsync((SavedFoodItem?)null);
        _factory.SavedFoodItems.Setup(r => r.CountByUserIdAsync(utilisateur.Id)).ReturnsAsync(10);

        var reponse = await _factory.CreateAuthenticatedClient()
            .PostAsJsonAsync(Favoris, new SaveFoodItemRequest(aliment.Id));

        // Free : 10 favoris au maximum.
        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
        _factory.SavedFoodItems.Verify(r => r.AddAsync(It.IsAny<SavedFoodItem>()), Times.Never);
    }

    [Fact]
    public async Task IT_USR_15_SuppressionDunFavori_Retourne204()
    {
        var utilisateur = GivenUtilisateurCourant();
        var favori = new SavedFoodItem(utilisateur.Id, Guid.NewGuid());
        _factory.SavedFoodItems.Setup(r => r.GetByIdAsync(favori.Id)).ReturnsAsync(favori);

        var reponse = await _factory.CreateAuthenticatedClient().DeleteAsync($"{Favoris}/{favori.Id}");

        Assert.Equal(HttpStatusCode.NoContent, reponse.StatusCode);
        _factory.SavedFoodItems.Verify(r => r.DeleteAsync(favori.Id), Times.Once);
    }

    [Fact]
    public async Task IT_USR_16_SuppressionDunFavoriInexistant_Retourne404()
    {
        GivenUtilisateurCourant();
        var inconnu = Guid.NewGuid();
        _factory.SavedFoodItems.Setup(r => r.GetByIdAsync(inconnu)).ReturnsAsync((SavedFoodItem?)null);

        var reponse = await _factory.CreateAuthenticatedClient().DeleteAsync($"{Favoris}/{inconnu}");

        Assert.Equal(HttpStatusCode.NotFound, reponse.StatusCode);
    }

    [Fact]
    public async Task SuppressionDuFavoriDunAutreUtilisateur_Retourne403()
    {
        GivenUtilisateurCourant();
        var favoriDunAutre = new SavedFoodItem(Guid.NewGuid(), Guid.NewGuid());
        _factory.SavedFoodItems.Setup(r => r.GetByIdAsync(favoriDunAutre.Id)).ReturnsAsync(favoriDunAutre);

        var reponse = await _factory.CreateAuthenticatedClient().DeleteAsync($"{Favoris}/{favoriDunAutre.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, reponse.StatusCode);
    }
}
