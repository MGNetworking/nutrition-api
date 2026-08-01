using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Interfaces.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RgpdController : ControllerBase
{
    private readonly IRgpdService _rgpdService;
    public RgpdController(IRgpdService rgpdService)
     => _rgpdService = rgpdService;

    /// <summary>Demande de suppression RGPD du compte utilisateur.</summary>
    [HttpDelete]
    [SwaggerOperation(
        Summary = "Demander la suppression du compte (RGPD Art. 17)",
        Description = "Marque le compte pour suppression avec une grace period de 30 jours, pendant laquelle il reste réactivable.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        await _rgpdService.DeleteUserAsync(userKcId);
        return NoContent();
    }

    /// <summary>Réactivation du compte pendant la période de grâce.</summary>
    [HttpPost("reactivate")]
    [SwaggerOperation(
        Summary = "Réactiver le compte pendant la grace period",
        Description = "Annule la demande de suppression RGPD si la grace period de 30 jours n'est pas écoulée.")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _rgpdService.ReactivateUserAsync(userKcId);
        return Ok(result);
    }

    /// <summary>Export des données personnelles RGPD sous forme de fichier ZIP.</summary>
    [HttpGet("export")]
    [SwaggerOperation(
        Summary = "Exporter ses données personnelles (RGPD Art. 20)",
        Description = "Retourne une archive ZIP contenant l'ensemble des données personnelles de l'utilisateur au format JSON.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserRgpdExportData()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var exportData = await _rgpdService.ExportUserDataAsync(userKcId);

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("User-Data.json");
            using var entryStream = entry.Open();
            entryStream.Write(jsonBytes);
        }

        return File(memoryStream.ToArray(), "application/zip", $"export-{DateTime.UtcNow:yyyy-MM-dd}.zip");
    }
}
