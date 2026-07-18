using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation(
        Summary = "Lancer un plan diététique",
        Description = "Crée une Diet active à partir du plan (snapshot). Échoue si une Diet est déjà active (409) ou si aucune pesée n'existe (422).")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [SwaggerOperation(
        Summary = "Récupérer le régime actif",
        Description = "Retourne la Diet au statut Active de l'utilisateur connecté (404 si aucune).")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActive()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.GetActiveAsync(userId);

        return Ok(result);
    }

    /// <summary>Historique des régimes de l'utilisateur.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Historique des régimes",
        Description = "Retourne tous les régimes de l'utilisateur connecté, actifs et archivés.")]
    [ProducesResponseType(typeof(List<DietResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetHistory()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.GetHistoryAsync(userId);

        return Ok(result);
    }

    /// <summary>Détail d'un régime.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Détail d'un régime",
        Description = "Retourne le détail d'un régime de l'utilisateur connecté.")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.GetByIdAsync(userId, id);

        return Ok(result);
    }

    /// <summary>Terminer le régime actif.</summary>
    [HttpPost("{id:guid}/archive")]
    [SwaggerOperation(
        Summary = "Terminer le régime actif",
        Description = "Archive le régime avec sa date de fin. Échoue si le régime n'est pas au statut Active (422).")]
    [ProducesResponseType(typeof(DietResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Archive([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _dietService.ArchiveAsync(userId, id);

        return Ok(result);
    }

}
