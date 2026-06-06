using System.Diagnostics.CodeAnalysis;

namespace NutritionApi.Application.DTOS.Users;

public record WeightEntryResponse(Guid Id, float Weight, DateOnly MeasuredAt);
