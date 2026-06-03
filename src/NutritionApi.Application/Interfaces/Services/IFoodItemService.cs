using NutritionApi.Application.DTOS.FoodItems;

namespace NutritionApi.Application.Interfaces.Services;


public interface IFoodItemService
{
    Task<List<FoodItemSearchResponse>> SearchAsync(string keyword, int limit = 20);
    Task<List<SavedFoodItemResponse>> GetSavedAsync(Guid userId);
    Task<SavedFoodItemResponse> SaveAsync(Guid userId, SaveFoodItemRequest request);
    Task RemoveSavedAsync(Guid userId, Guid savedId);
}
