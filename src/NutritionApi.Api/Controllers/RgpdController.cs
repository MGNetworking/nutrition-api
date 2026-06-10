using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.Interfaces.Services;

using System.Security.Claims;


namespace NutritionApi.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class RgpdController : ControllerBase
{
    private readonly IRgpdService _rgpdService;
    public RgpdController(IRgpdService rgpdService)
     => _rgpdService = rgpdService;

    [HttpGet("me")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public async Task<UserExportResponse> GetUserRgpdExportData()
    {
        var userKcId = HttpContext.User.FindFirstValue("sub")!;
        return await _rgpdService.ExportUserDataAsync(userKcId);
    }
}
