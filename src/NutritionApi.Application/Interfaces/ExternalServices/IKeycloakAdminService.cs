namespace NutritionApi.Application.Interfaces.ExternalServices;

public interface IKeycloakAdminService
{
    Task DisableUserAsync(string keycloakId);
    Task EnableUserAsync(string keycloakId);
    Task DeleteUserAsync(string keycloakId);
}
