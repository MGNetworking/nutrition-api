namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

public interface IFoodItemRepository
{
    Task<FoodItem?> GetByIdAsync(Guid id);
    Task<List<FoodItem>> GetByIdsAsync(List<Guid> ids);
    Task<FoodItem?> GetByOffIdAsync(string offId);
    Task<List<FoodItem>> SearchByKeywordAsync(string keyword, int limit = 20);
    Task AddAsync(FoodItem foodItem);
    Task UpdateAsync(FoodItem foodItem);
    Task<int> CountAsync();
}
