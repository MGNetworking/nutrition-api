namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Users;

public interface IRgpdService
{
    Task<UserExportResponse> ExportUserDataAsync(string keycloakId);
}
