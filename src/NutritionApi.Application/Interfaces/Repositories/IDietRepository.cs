namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

public interface IDietRepository
{
    Task<Diet?> GetByIdAsync(Guid id);
    Task<Diet?> GetActiveByUserIdAsync(Guid userId);
    Task<List<Diet>> GetByUserIdAsync(Guid userId);
    Task AddAsync(Diet diet);
    Task UpdateAsync(Diet diet);
}
