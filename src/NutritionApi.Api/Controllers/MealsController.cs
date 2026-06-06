using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.Interfaces.Services;

namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/meals")]
[Authorize]
public class MealsController : ControllerBase
{
    private readonly IMealService _mealService;

    public MealsController(IMealService mealService)
    {
        _mealService = mealService;
    }

    /// <summary>Créer un repas.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateMealRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.CreateAsync(userId,request);

        return Created(string.Empty, result);
    }

    /// <summary>Lister les repas de l'utilisateur.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<MealResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? saved,
        [FromQuery] DateOnly? date)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.GetAllAsync(userId, saved, date);

        return Ok(result);
    }

    /// <summary>Détail d'un repas.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.GetByIdAsync(userId, id);

        return Ok(result);
    }

    /// <summary>Modifier les métadonnées d'un repas.</summary>
    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMealRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.UpdateAsync(userId, id, request);

        return Ok(result);
    }

    /// <summary>Supprimer un repas.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _mealService.DeleteAsync(userId, id);

        return NoContent();
    }

    /// <summary>Ajouter un aliment à un repas.</summary>
    [HttpPost("{id:guid}/items")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem([FromRoute] Guid id, [FromBody] AddMealItemRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.AddItemAsync(userId, id, request);

        return Created(string.Empty, result);
    }

    /// <summary>Retirer un aliment d'un repas.</summary>
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem([FromRoute] Guid id, [FromRoute] Guid itemId)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _mealService.RemoveItemAsync(userId, id, itemId);

        return NoContent();
    }
}
