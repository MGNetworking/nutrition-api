namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

public interface ISavedFoodItemRepository
{
    Task<SavedFoodItem?> GetByIdAsync(Guid id);
    Task<SavedFoodItem?> GetByUserIdAndFoodItemIdAsync(Guid userId, Guid foodItemId);
    Task<List<SavedFoodItem>> GetByUserIdAsync(Guid userId);
    Task AddAsync(SavedFoodItem savedFoodItem);
    Task DeleteAsync(Guid id);
    Task<int> CountByUserIdAsync(Guid userId);
}
