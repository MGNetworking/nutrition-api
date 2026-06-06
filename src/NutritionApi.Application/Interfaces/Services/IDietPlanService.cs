namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.DTOS.Diets;

public interface IDietPlanService
{
    Task<List<DietPlanResponse>> GetUserPlansAsync(Guid userId);
    Task<DietPlanResponse> CreateAsync(Guid userId, CreateDietPlanRequest request);
    Task<DietPlanResponse> UpdateAsync(Guid userId, Guid planId, UpdateDietPlanRequest request);
    Task DeleteAsync(Guid userId, Guid planId);
    Task<DietResponse> LaunchAsync(Guid userId, Guid planId);
    Task<List<DietPlanResponse>> GetTemplatesAsync(Guid userId);
}
