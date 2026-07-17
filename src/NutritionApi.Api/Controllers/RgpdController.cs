using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Interfaces.Services;
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        await _rgpdService.DeleteUserAsync(userKcId);
        return NoContent();
    }

    /// <summary>Réactivation du compte pendant la période de grâce.</summary>
    [HttpPost("reactivate")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reactivate()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        var result = await _rgpdService.ReactivateUserAsync(userKcId);
        return Ok(result);
    }

    /// <summary>Export des données personnelles RGPD sous forme de fichier ZIP.</summary>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
