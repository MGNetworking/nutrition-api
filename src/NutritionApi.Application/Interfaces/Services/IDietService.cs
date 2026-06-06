namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.Nutrition;

public interface IDietService
{
    Task<DietResponse> GetActiveAsync(Guid userId);
    Task<List<DietResponse>> GetHistoryAsync(Guid userId);
    Task<DietResponse> GetByIdAsync(Guid userId, Guid dietId);
    Task<DietResponse> ArchiveAsync(Guid userId, Guid dietId);
    Task<NutritionBilanResponse> GetBilanAsync(Guid userId, Guid dietId, string period, DateOnly? date, DateOnly? startDate, DateOnly? endDate);
}
