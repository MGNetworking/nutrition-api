using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.Interfaces.Repositories;

public interface IWeightEntryRepository
{
    Task<WeightEntry?> GetByIdAsync(Guid id);
    Task<WeightEntry?> GetByUserIdAndDateAsync(Guid userId, DateOnly date);
    Task<List<WeightEntry>> GetByUserIdAsync(Guid userId);
    Task AddAsync(WeightEntry entry);
    Task UpdateAsync(WeightEntry entry);
}
