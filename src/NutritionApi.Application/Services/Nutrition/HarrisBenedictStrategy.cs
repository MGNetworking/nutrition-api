using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Services.Nutrition;

public sealed class HarrisBenedictStrategy : IBmrStrategy
{
    public float Calculate(User user, float weightKg)
    {
        var age = ComputeAge(user.BirthDate);

        return user.Gender == Gender.Male
            ? 88.362f + 13.397f * weightKg + 4.799f * user.Height - 5.677f * age
            : 447.593f + 9.247f * weightKg + 3.098f * user.Height - 4.330f * age;
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
