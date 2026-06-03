
namespace NutritionApi.Application.DTOS.Users;

public record AddWeightEntryRequest(float Weight, DateOnly? MeasuredAt = null);
