namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

public interface IMealRepository
{
    Task<Meal?> GetByIdAsync(Guid id);
    Task<List<Meal>> GetByUserIdAsync(Guid userId, DateOnly? date = null, bool? saved = null);
    Task AddAsync(Meal meal);
    Task UpdateAsync(Meal meal);
    Task DeleteAsync(Guid id);
    Task<int> CountSavedByUserIdAsync(Guid userId);
}
