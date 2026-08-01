using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Api.Middleware;
using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace NutritionApi.Api.Controllers;

/// <remarks>
/// <see cref="AllowWithoutProfileAttribute"/> porte sur le controller entier : administrer
/// l'application et en être client sont deux choses distinctes. La table <c>users</c> contient un
/// profil nutritionnel — date de naissance, taille, allergies — dont un administrateur n'a que
/// faire. Sans cette dispense, aucun de ces endpoints n'était atteignable tant qu'il n'en possédait
/// pas un, alors que son rôle Keycloak suffit à l'autoriser.
/// </remarks>
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "admin")]
[AllowWithoutProfile]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>KPIs consolidés du dashboard admin.</summary>
    [HttpGet("dashboard")]
    [SwaggerOperation(
        Summary = "KPIs du dashboard admin",
        Description = "Retourne les indicateurs consolidés : utilisateurs par tier, nouveaux comptes 7j, diets actives, repas 7j, comptes en grace period. Rôle admin requis.")]
    [ProducesResponseType(typeof(AdminDashboardResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _adminService.GetDashboardAsync();
        return Ok(result);
    }

    /// <summary>Statut des jobs Hangfire et compteur d'aliments.</summary>
    [HttpGet("system/health")]
    [SwaggerOperation(
        Summary = "État de santé du système",
        Description = "Retourne le statut des jobs planifiés (import Open Food Facts, purge RGPD), la date du dernier import et le nombre de FoodItems. Rôle admin requis.")]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSystemHealth()
    {
        var result = await _adminService.GetSystemHealthAsync();
        return Ok(result);
    }

    /// <summary>Créer un template de plan diététique.</summary>
    [HttpPost("diet-plans/templates")]
    [SwaggerOperation(
        Summary = "Créer un template de plan",
        Description = "Crée un plan template partagé, sans propriétaire, mis à disposition des utilisateurs Pro/Business. Rôle admin requis.")]
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateDietPlanRequest request)
    {
        var result = await _adminService.CreateTemplateAsync(request);
        return Created(string.Empty, result);
    }

    /// <summary>Modifier un template de plan diététique.</summary>
    [HttpPut("diet-plans/templates/{id:guid}")]
    [SwaggerOperation(
        Summary = "Modifier un template de plan",
        Description = "Met à jour un plan template existant. Un plan personnel n'est pas atteignable par cette route (404). Rôle admin requis.")]
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate([FromRoute] Guid id, [FromBody] UpdateDietPlanRequest request)
    {
        var result = await _adminService.UpdateTemplateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Supprimer un template de plan diététique.</summary>
    [HttpDelete("diet-plans/templates/{id:guid}")]
    [SwaggerOperation(
        Summary = "Supprimer un template de plan",
        Description = "Supprime un plan template. Un plan personnel n'est pas atteignable par cette route (404). Rôle admin requis.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate([FromRoute] Guid id)
    {
        await _adminService.DeleteTemplateAsync(id);
        return NoContent();
    }
}
