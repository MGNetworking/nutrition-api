using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Extensions;
using NutritionApi.Api.Middleware;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;
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
    // Seul endpoint appelable sans profil existant : c'est celui qui le crée.
    [AllowWithoutProfile]
    [SwaggerOperation(
        Summary = "Créer le profil utilisateur",
        Description = "Crée le profil à la première connexion Keycloak, avec la première pesée (WeightEntry) issue du poids déclaré.")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserProfileRequest createUser)
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _userService.CreateUserProfileAsync(userKcId, createUser);
        return CreatedAtAction(nameof(GetProfile), result);
    }


    /// <summary>Récupérer le profil utilisateur</summary>
    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Lire le profil utilisateur",
        Description = "Retourne le profil de l'utilisateur connecté, avec BMR et TDEE calculés à la demande.")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _userService.GetUserProfileAsync(userKcId);

        return Ok(result);
    }

    [HttpPut("me")]
    [SwaggerOperation(
        Summary = "Mettre à jour le profil utilisateur",
        Description = "Met à jour les données biométriques et les préférences de l'utilisateur connecté.")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutProfile([FromBody] UpdateUserProfileRequest updateUser)
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _userService.UpdateUserProfileAsync(userKcId, updateUser);

        return Ok(result);
    }

    [HttpPost("me/weight-entries")]
    [SwaggerOperation(
        Summary = "Ajouter une pesée",
        Description = "Enregistre une nouvelle pesée (WeightEntry) pour l'utilisateur connecté. Une seule pesée par date.")]
    [ProducesResponseType(typeof(WeightEntryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddWeightEntry([FromBody] AddWeightEntryRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var weight = await _userService.AddWeightEntryAsync(userId, request);

        return CreatedAtAction(nameof(GetProfile), weight);
    }

    [HttpGet("me/weight-entries")]
    [SwaggerOperation(
        Summary = "Historique des pesées",
        Description = "Retourne l'historique complet des pesées de l'utilisateur connecté.")]
    [ProducesResponseType(typeof(List<WeightEntryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetWeightHistory()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var listWeight = await _userService.GetWeightHistoryAsync(userId);

        return Ok(listWeight);
    }

    [HttpPut("me/weight-entries/{id:guid}")]
    [SwaggerOperation(
        Summary = "Modifier une pesée",
        Description = "Met à jour une pesée existante de l'utilisateur connecté (poids et/ou date).")]
    [ProducesResponseType(typeof(WeightEntryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateWeightEntry(Guid id, [FromBody] UpdateWeightEntryRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var weight = await _userService.UpdateWeightEntryAsync(userId, id, request);

        return Ok(weight);

    }

    [HttpGet("me/saved-food-items")]
    [SwaggerOperation(
        Summary = "Liste des aliments favoris",
        Description = "Retourne la liste personnelle d'aliments sauvegardés de l'utilisateur connecté.")]
    [ProducesResponseType(typeof(List<SavedFoodItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSavedFoodItems()
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var lsFood = await _foodItemService.GetSavedAsync(userId);

        return Ok(lsFood);
    }

    [HttpPost("me/saved-food-items")]
    [SwaggerOperation(
        Summary = "Sauvegarder un aliment en favori",
        Description = "Ajoute un aliment à la liste personnelle de l'utilisateur connecté.")]
    [ProducesResponseType(typeof(SavedFoodItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SaveFoodItem([FromBody] SaveFoodItemRequest request)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        var item = await _foodItemService.SaveAsync(userId, request);

        return CreatedAtAction(nameof(GetSavedFoodItems), item);
    }

    [HttpDelete("me/saved-food-items/{id:guid}")]
    [SwaggerOperation(
        Summary = "Retirer un aliment des favoris",
        Description = "Supprime un aliment de la liste personnelle de l'utilisateur connecté.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSavedFoodItem(Guid id)
    {
        var userId = UserContextExtensions.GetUserId(HttpContext);
        await _foodItemService.RemoveSavedAsync(userId, id);

        return NoContent();
    }
}
