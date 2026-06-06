namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

public interface IDietPlanRepository
{
    Task<DietPlan?> GetByIdAsync(Guid id);
    Task<List<DietPlan>> GetByUserIdAsync(Guid userId);
    Task<List<DietPlan>> GetTemplatesAsync();
    Task AddAsync(DietPlan dietPlan);
    Task UpdateAsync(DietPlan dietPlan);
    Task DeleteAsync(Guid id);
    Task<int> CountByUserIdAsync(Guid userId);
}
