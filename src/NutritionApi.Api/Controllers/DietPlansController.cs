using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/diet-plans")]
[Authorize]
public class DietPlansController : ControllerBase
{
    private readonly IDietPlanService _dietPlanService;

    public DietPlansController(IDietPlanService dietPlanService)
    {
        _dietPlanService = dietPlanService;
    }

    /// <summary>Lister les plans personnels de l'utilisateur.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Lister ses plans personnels",
        Description = "Retourne tous les plans diététiques personnels de l'utilisateur connecté.")]
    [ProducesResponseType(typeof(List<DietPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var lsPlan = await _dietPlanService.GetUserPlansAsync(userId);

        return Ok(lsPlan);
    }

    /// <summary>Créer un plan diététique personnel.</summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Créer un plan diététique",
        Description = "Crée un plan personnel (type de diète, objectif, poids cible, répartition des macros). Le quota de plans dépend du tier d'abonnement.")]
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateDietPlanRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var planDiet = await _dietPlanService.CreateAsync(userId, request);

        return Created(string.Empty, planDiet);
    }

    /// <summary>Modifier un plan diététique personnel.</summary>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Modifier un plan diététique",
        Description = "Met à jour un plan personnel de l'utilisateur connecté. Un plan appartenant à un autre utilisateur est introuvable (404).")]
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDietPlanRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietPlanService.UpdateAsync(userId, id, request);

        return Ok(result);
    }

    /// <summary>Supprimer un plan diététique personnel.</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Supprimer un plan diététique",
        Description = "Supprime un plan personnel de l'utilisateur connecté.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _dietPlanService.DeleteAsync(userId, id);

        return NoContent();
    }

    /// <summary>Lister les templates partagés (Pro/Business).</summary>
    [HttpGet("templates")]
    [SwaggerOperation(
        Summary = "Lister les templates partagés",
        Description = "Retourne les plans templates mis à disposition par les admins. Réservé aux abonnements Pro et Business (403 sinon).")]
    [ProducesResponseType(typeof(List<DietPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplates()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietPlanService.GetTemplatesAsync(userId);

        return Ok(result);
    }
}
