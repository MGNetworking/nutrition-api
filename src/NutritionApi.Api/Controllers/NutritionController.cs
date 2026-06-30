using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.Nutrition;
using NutritionApi.Application.Interfaces.Services;

namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/nutrition")]
[Authorize]
public class NutritionController : ControllerBase
{
    private readonly INutritionService _nutritionService;

    public NutritionController(INutritionService nutritionService)
    {
        _nutritionService = nutritionService;
    }

    /// <summary>Bilan nutritionnel d'un régime sur une période donnée.</summary>
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
        var result = await _nutritionService.GetBilanAsync(userId, id, period, date, startDate, endDate);
        return Ok(result);
    }
}
