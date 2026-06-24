using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.Nutrition;
using NutritionApi.Application.Interfaces.Services;

namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/diets")]
[Authorize]
public class DietsController : ControllerBase
{
    private readonly IDietService _dietService;

    public DietsController(IDietService dietService)
    {
        _dietService = dietService;
    }

    /// <summary>Lancer un plan → crée une Diet active.</summary>
    [HttpPost("{id:guid}/launch")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Launch([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.LaunchAsync(userId, id);

        return Created(string.Empty, result);
    }

    /// <summary>Récupérer le régime actif de l'utilisateur.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActive()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.GetActiveAsync(userId);

        return Ok(result);
    }

    /// <summary>Historique des régimes de l'utilisateur.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<DietResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.GetHistoryAsync(userId);

        return Ok(result);
    }

    /// <summary>Détail d'un régime.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.GetByIdAsync(userId, id);

        return Ok(result);
    }

    /// <summary>Terminer le régime actif.</summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Archive([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.ArchiveAsync(userId, id);

        return Ok(result);
    }

    /// <summary>Bilan nutritionnel d'un régime.</summary>
    [HttpGet("{id:guid}/bilan")]
    [ProducesResponseType(typeof(NutritionBilanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetBilan(
        [FromRoute] Guid id,
        [FromQuery] string period,
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.GetBilanAsync(userId, id, period, date, startDate, endDate);

        return Ok(result);
    }
}
