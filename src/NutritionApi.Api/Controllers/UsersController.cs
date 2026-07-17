using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Interfaces.Services;
using System.Security.Claims;


namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IFoodItemService _foodItemService;

    public UsersController(IUserService userService, IFoodItemService foodItemService)
    {
        _userService = userService;
        _foodItemService = foodItemService;
    }


    /// <summary>Création de l'utilisateur</summary>
    [HttpPost("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserProfileRequest createUser)
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _userService.CreateUserProfileAsync(userKcId, createUser);
        return CreatedAtAction(nameof(GetProfile), result);
    }


    /// <summary>Récupérer le profil utilisateur</summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _userService.GetUserProfileAsync(userKcId);

        return Ok(result);
    }

    [HttpPut("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutProfile([FromBody] UpdateUserProfileRequest updateUser)
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _userService.UpdateUserProfileAsync(userKcId, updateUser);

        return Ok(result);
    }

    [HttpPost("me/weight-entries")]
    [ProducesResponseType(typeof(WeightEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddWeightEntry([FromBody] AddWeightEntryRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var weight = await _userService.AddWeightEntryAsync(userId, request);

        return CreatedAtAction(nameof(GetProfile), weight);
    }

    [HttpGet("me/weight-entries")]
    [ProducesResponseType(typeof(List<WeightEntryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWeightHistory()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var listWeight = await _userService.GetWeightHistoryAsync(userId);

        return Ok(listWeight);
    }

    [HttpPut("me/weight-entries/{id:guid}")]
    [ProducesResponseType(typeof(WeightEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateWeightEntry(Guid id, [FromBody] UpdateWeightEntryRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var weight = await _userService.UpdateWeightEntryAsync(userId, id, request);

        return Ok(weight);

    }

    [HttpGet("me/saved-food-items")]
    [ProducesResponseType(typeof(List<SavedFoodItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSavedFoodItems()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var lsFood = await _foodItemService.GetSavedAsync(userId);

        return Ok(lsFood);
    }

    [HttpPost("me/saved-food-items")]
    [ProducesResponseType(typeof(SavedFoodItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveFoodItem([FromBody] SaveFoodItemRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var item = await _foodItemService.SaveAsync(userId, request);

        return CreatedAtAction(nameof(GetSavedFoodItems), item);
    }

    [HttpDelete("me/saved-food-items/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSavedFoodItem(Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _foodItemService.RemoveSavedAsync(userId, id);

        return NoContent();
    }
}
