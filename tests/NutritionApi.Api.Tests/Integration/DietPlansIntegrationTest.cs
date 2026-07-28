namespace NutritionApi.Api.Tests.Integration;

// Tests de NIVEAU 2 — pipeline HTTP (NTR-29 / NTR-108).
//
// Prérequis avant d'implémenter :
//   - ApiFactory (Integration/Fixtures/) — WebApplicationFactory<Program>
//   - TestAuthHandler — pas de Keycloak à ce niveau
//   - Doublures des repositories et de IUnitOfWork, armées par test
//
// Ce qui est réel ici : routing, model binding, ExceptionMiddleware,
// UserResolutionMiddleware, autorisation, sérialisation, codes de statut.
// Ce qui ne l'est pas : PostgreSQL, Redis, Keycloak — ils appartiennent au
// niveau 3 (NTR-28). Un test qui a besoin d'eux n'a pas sa place dans ce fichier.
//
// L'en-tête précédent annonçait docker-compose et des JWT signés : c'était une
// description de niveau 3, corrigée le 2026-07-27 après arbitrage de la frontière
// entre niveaux 2 et 3.
//
// Référence : docs/pages/backend/qualite/recensement-des-tests.md — section DietPlansController

public class DietPlansIntegrationTest
{
    // -------------------------------------------------------------------------
    // UserResolutionMiddleware — IT-DP-01
    // -------------------------------------------------------------------------

    [Fact(Skip = "integration — IT-DP-01 : vérifie que UserResolutionMiddleware injecte le bon UserId via le pipeline réel")]
    public async Task GetAll_WithRealMiddleware_UsesUserIdFromHttpContext() { }

    // -------------------------------------------------------------------------
    // GET /api/v1/diet-plans
    // -------------------------------------------------------------------------

    [Fact(Skip = "integration — IT-DP-01 : retourne 200 avec les plans de l'utilisateur authentifié")]
    public async Task GetAll_WhenAuthenticated_Returns200() { }

    [Fact(Skip = "integration — IT-AUTH-01 : retourne 401 si token absent")]
    public async Task GetAll_WithoutToken_Returns401() { }

    // -------------------------------------------------------------------------
    // POST /api/v1/diet-plans
    // -------------------------------------------------------------------------

    [Fact(Skip = "integration — IT-DP-02 : body valide → 201 avec plan créé en DB")]
    public async Task Create_WhenValid_Returns201() { }

    [Fact(Skip = "integration — IT-DP-03 : body invalide → 422 via ExceptionMiddleware")]
    public async Task Create_WhenInvalid_Returns422() { }

    // -------------------------------------------------------------------------
    // PUT /api/v1/diet-plans/{id}
    // -------------------------------------------------------------------------

    [Fact(Skip = "integration — IT-DP-04 : plan existant et ownership → 200")]
    public async Task Update_WhenOwner_Returns200() { }

    [Fact(Skip = "integration — IT-DP-05 : plan appartenant à un autre user → 403")]
    public async Task Update_WhenNotOwner_Returns403() { }

    [Fact(Skip = "integration — IT-DP-06 : plan inexistant → 404")]
    public async Task Update_WhenNotFound_Returns404() { }

    // -------------------------------------------------------------------------
    // DELETE /api/v1/diet-plans/{id}
    // -------------------------------------------------------------------------

    [Fact(Skip = "integration — IT-DP-07 : plan existant et ownership → 204")]
    public async Task Delete_WhenOwner_Returns204() { }

    [Fact(Skip = "integration — IT-DP-08 : plan inexistant → 404")]
    public async Task Delete_WhenNotFound_Returns404() { }

    // -------------------------------------------------------------------------
    // POST /api/v1/diets/{id}/launch  — porté par DietsController, d'où les IDs IT-DT-*
    // -------------------------------------------------------------------------

    [Fact(Skip = "integration — IT-DT-09 : plan valide, pas de Diet active → 201 Diet créée")]
    public async Task Launch_WhenNoDietActive_Returns201() { }

    [Fact(Skip = "integration — IT-DT-10 : Diet déjà active → 409 (règle métier)")]
    public async Task Launch_WhenDietAlreadyActive_Returns409() { }

    [Fact(Skip = "integration — IT-DT-11 : plan inexistant → 404")]
    public async Task Launch_WhenPlanNotFound_Returns404() { }

    [Fact(Skip = "integration — IT-DT-12 : données du plan insuffisantes → 422")]
    public async Task Launch_WhenPlanInvalid_Returns422() { }

    // -------------------------------------------------------------------------
    // GET /api/v1/diet-plans/templates
    // -------------------------------------------------------------------------

    [Fact(Skip = "integration — IT-DP-09 : user Free → 403")]
    public async Task GetTemplates_WhenFreeUser_Returns403() { }

    [Fact(Skip = "integration — IT-DP-10 : user Pro/Business → 200 avec templates")]
    public async Task GetTemplates_WhenProUser_Returns200() { }
}
