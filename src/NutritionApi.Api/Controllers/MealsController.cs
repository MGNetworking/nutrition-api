using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;

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
    [SwaggerOperation(
        Summary = "Créer un repas",
        Description = "Crée un repas ponctuel ou sauvegardé avec ses MealItems. Le quota de repas sauvegardés dépend du tier d'abonnement (403).")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [SwaggerOperation(
        Summary = "Lister ses repas",
        Description = "Retourne les repas de l'utilisateur connecté, filtrables par repas sauvegardés (?saved=true) et par date de consommation (?date=).")]
    [ProducesResponseType(typeof(List<MealResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [SwaggerOperation(
        Summary = "Détail d'un repas",
        Description = "Retourne un repas de l'utilisateur connecté avec ses MealItems et ses valeurs nutritionnelles.")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.GetByIdAsync(userId, id);

        return Ok(result);
    }

    /// <summary>Modifier les métadonnées d'un repas.</summary>
    [HttpPatch("{id:guid}")]
    [SwaggerOperation(
        Summary = "Modifier un repas",
        Description = "Met à jour les propriétés d'un repas : name, mealType, notes, consumedAt. Les MealItems se gèrent via les routes /items.")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMealRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.UpdateAsync(userId, id, request);

        return Ok(result);
    }

    /// <summary>Supprimer un repas.</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Supprimer un repas",
        Description = "Supprime un repas de l'utilisateur connecté et ses MealItems.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _mealService.DeleteAsync(userId, id);

        return NoContent();
    }

    /// <summary>Ajouter un aliment à un repas.</summary>
    [HttpPost("{id:guid}/items")]
    [SwaggerOperation(
        Summary = "Ajouter un MealItem",
        Description = "Ajoute un aliment (FoodItem + quantité) à un repas existant et recalcule les valeurs nutritionnelles.")]
    [ProducesResponseType(typeof(MealResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem([FromRoute] Guid id, [FromBody] AddMealItemRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var result = await _mealService.AddItemAsync(userId, id, request);

        return Created(string.Empty, result);
    }

    /// <summary>Retirer un aliment d'un repas.</summary>
    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [SwaggerOperation(
        Summary = "Retirer un MealItem",
        Description = "Retire un aliment d'un repas existant et recalcule les valeurs nutritionnelles.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem([FromRoute] Guid id, [FromRoute] Guid itemId)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _mealService.RemoveItemAsync(userId, id, itemId);

        return NoContent();
    }
}
