using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/food-items")]
[Authorize]
public class FoodItemsController : ControllerBase
{
    private readonly IFoodItemService _foodItemService;

    public FoodItemsController(IFoodItemService foodItemService)
    {
        _foodItemService = foodItemService;
    }

    /// <summary>Rechercher un aliment par mot-clé.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Rechercher un aliment",
        Description = "Recherche dans le catalogue d'aliments par mot-clé (?search=), avec limite de résultats (?limit=, 20 par défaut).")]
    [ProducesResponseType(typeof(List<FoodItemSearchResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Search(
        [FromQuery] string search,
        [FromQuery] int limit = 20)
    {
        var result = await _foodItemService.SearchAsync(search, limit);

        return Ok(result);
    }
}
