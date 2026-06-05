using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.Interfaces.Services;

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
    [ProducesResponseType(typeof(List<FoodItemSearchResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string search,
        [FromQuery] int limit = 20)
    {
        var result = await _foodItemService.SearchAsync(search, limit);

        return Ok(result);
    }
}
