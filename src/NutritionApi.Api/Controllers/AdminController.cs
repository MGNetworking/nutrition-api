using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Interfaces.Services;

namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>KPIs consolidés du dashboard admin.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _adminService.GetDashboardAsync();
        return Ok(result);
    }

    /// <summary>Statut des jobs Hangfire et compteur d'aliments.</summary>
    [HttpGet("system/health")]
    [ProducesResponseType(typeof(SystemHealthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSystemHealth()
    {
        var result = await _adminService.GetSystemHealthAsync();
        return Ok(result);
    }

    /// <summary>Créer un template de plan diététique.</summary>
    [HttpPost("diet-plans/templates")]
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateDietPlanRequest request)
    {
        var result = await _adminService.CreateTemplateAsync(request);
        return Created(string.Empty, result);
    }

    /// <summary>Modifier un template de plan diététique.</summary>
    [HttpPut("diet-plans/templates/{id:guid}")]
    [ProducesResponseType(typeof(DietPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate([FromRoute] Guid id, [FromBody] UpdateDietPlanRequest request)
    {
        var result = await _adminService.UpdateTemplateAsync(id, request);
        return Ok(result);
    }

    /// <summary>Supprimer un template de plan diététique.</summary>
    [HttpDelete("diet-plans/templates/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTemplate([FromRoute] Guid id)
    {
        await _adminService.DeleteTemplateAsync(id);
        return NoContent();
    }
}
