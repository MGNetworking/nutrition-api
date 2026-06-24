using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Interfaces.Services;

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
    [ProducesResponseType(typeof(List<DietPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var lsPlan = await _dietPlanService.GetUserPlansAsync(userId);

        return Ok(lsPlan);
    }

    /// <summary>Créer un plan diététique personnel.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status201Created)]
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
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateDietPlanRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietPlanService.UpdateAsync(userId, id, request);

        return Ok(result);
    }

    /// <summary>Supprimer un plan diététique personnel.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _dietPlanService.DeleteAsync(userId, id);

        return NoContent();
    }

    /// <summary>Lister les templates partagés (Pro/Business).</summary>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(List<DietPlanResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTemplates()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietPlanService.GetTemplatesAsync(userId);

        return Ok(result);
    }
}
