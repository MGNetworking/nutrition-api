namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;

public interface IAdminService
{
    Task<AdminDashboardResponse> GetDashboardAsync();
    Task<SystemHealthResponse> GetSystemHealthAsync();
    Task<DietPlanResponse> CreateTemplateAsync(CreateDietPlanRequest request);
    Task<DietPlanResponse> UpdateTemplateAsync(Guid templateId, UpdateDietPlanRequest request);
    Task DeleteTemplateAsync(Guid templateId);
}
