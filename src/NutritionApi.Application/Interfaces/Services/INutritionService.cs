namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Nutrition;

public interface INutritionService
{
    Task<NutritionBilanResponse> GetBilanAsync(Guid userId, Guid dietId, string period, DateOnly? date, DateOnly? startDate, DateOnly? endDate);
}
