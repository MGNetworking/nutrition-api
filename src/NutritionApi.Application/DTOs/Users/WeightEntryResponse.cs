using NutritionApi.Domain.Entity;
using System.Diagnostics.CodeAnalysis;

namespace NutritionApi.Application.DTOS.Users;

public record WeightEntryResponse(Guid Id, float Weight, DateOnly MeasuredAt)
{
    public static WeightEntryResponse From(WeightEntry weight)
        => new(weight.Id, weight.Weight, weight.MeasuredAt);
}
