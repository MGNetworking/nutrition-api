namespace NutritionApi.Api.Tests.Integration;

// Prérequis avant d'implémenter :
//   - WebApplicationFactory<Program> + appsettings.Testing.json
//   - docker-compose (PostgreSQL, Redis, Keycloak réels — décision du 2026-07-21)
//   - Helper de génération de JWT signé avec la clé de test
//   - Méthodes SeedAsync() pour pré-charger les fixtures
//
// Référence : docs/pages/backend/features/interne/recensement-des-tests.md — section DietPlansController

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
