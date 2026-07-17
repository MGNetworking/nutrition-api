using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Services.Nutrition;

public sealed class MifflinStJeorStrategy : IBmrStrategy
{
    public float Calculate(User user, float weightKg)
    {
        var age = ComputeAge(user.BirthDate);

        return user.Gender == Gender.Male
            ? 10f * weightKg + 6.25f * user.Height - 5f * age + 5f
            : 10f * weightKg + 6.25f * user.Height - 5f * age - 161f;
    }

    private static int ComputeAge(DateOnly birthDate)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - birthDate.Year;
        if (birthDate.AddYears(age) > today)
            age--;
        return age;
    }
}
