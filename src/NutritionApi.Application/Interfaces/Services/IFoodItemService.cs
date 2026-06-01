namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.FoodItems;

public interface IFoodItemService
{
    Task<List<FoodItemSearchResponse>> SearchAsync(string keyword, int limit = 20);
    Task<List<SavedFoodItemResponse>> GetSavedAsync(Guid userId);
    Task<SavedFoodItemResponse> SaveAsync(Guid userId, SaveFoodItemRequest request);
    Task RemoveSavedAsync(Guid userId, Guid savedId);
}
