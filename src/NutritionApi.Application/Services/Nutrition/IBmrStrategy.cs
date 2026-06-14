using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.Services.Nutrition;

public interface IBmrStrategy
{
    float Calculate(User user, float weightKg);
}
