namespace NutritionApi.Application.DTOS.Users;

public record UpdateWeightEntryRequest(float Weight, DateOnly MeasuredAt);
