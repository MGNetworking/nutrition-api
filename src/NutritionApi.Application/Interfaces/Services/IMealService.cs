namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Meals;

public interface IMealService
{
    Task<MealResponse> CreateAsync(Guid userId, CreateMealRequest request);
    Task<List<MealResponse>> GetAllAsync(Guid userId, bool? saved, DateOnly? date);
    Task<MealResponse> GetByIdAsync(Guid userId, Guid mealId);
    Task<MealResponse> UpdateAsync(Guid userId, Guid mealId, UpdateMealRequest request);
    Task DeleteAsync(Guid userId, Guid mealId);
    Task<MealResponse> AddItemAsync(Guid userId, Guid mealId, AddMealItemRequest request);
    Task<MealResponse> RemoveItemAsync(Guid userId, Guid mealId, Guid itemId);
}
