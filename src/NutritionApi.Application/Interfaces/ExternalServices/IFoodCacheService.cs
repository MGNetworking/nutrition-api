namespace NutritionApi.Application.Interfaces.ExternalServices;

using NutritionApi.Application.DTOS.FoodItems;

public interface IFoodCacheService
{
    Task<List<FoodItemSearchResponse>?> GetAsync(string keyword);
    Task SetAsync(string keyword, List<FoodItemSearchResponse> results);
    Task InvalidateAsync(string keyword);
}
