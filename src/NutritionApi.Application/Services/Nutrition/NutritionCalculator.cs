using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Application.Services.Nutrition;

public sealed class NutritionCalculator
{
    private readonly IBmrStrategy _bmrStrategy;

    internal NutritionCalculator(IBmrStrategy bmrStrategy)
    {
        _bmrStrategy = bmrStrategy;
    }

    public NutritionNeeds Calculate(User user, float weightKg, Goal goal, MacroDistribution macros)
    {
        var bmr = _bmrStrategy.Calculate(user, weightKg);

        var nap = user.ActivityLevel switch
        {
            ActivityLevel.Sedentary => 1.2f,
            ActivityLevel.LightlyActive => 1.375f,
            ActivityLevel.ModeratelyActive => 1.55f,
            ActivityLevel.VeryActive => 1.725f,
            ActivityLevel.ExtremelyActive => 1.9f,
            _ => throw new ArgumentException($"Unknown activity level. Received: {user.ActivityLevel}", nameof(user.ActivityLevel))
        };

        var tdee = bmr * nap;

        var target = goal switch
        {
            Goal.WeightLoss => tdee - Math.Min(tdee * 0.15f, 500f),
            Goal.Maintenance => tdee,
            Goal.WeightGain => tdee + Math.Min(tdee * 0.15f, 500f),
            _ => throw new ArgumentException($"Unknown goal. Received: {goal}", nameof(goal))
        };

        return new NutritionNeeds(bmr, tdee, target, macros);
    }

    public static NutritionInfo CalculateNutrition(FoodItem foodItem, float quantity)
    => new(
        foodItem.CaloriesPer100g * quantity / 100f,
        (int)(foodItem.ProteinsPer100g * quantity / 100f),
        (int)(foodItem.CarbsPer100g * quantity / 100f),
        (int)(foodItem.FatsPer100g * quantity / 100f));


    public static MacroGrams ToGrams(MacroDistribution macros, int calorieTarget)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(calorieTarget);

        return new MacroGrams(
            proteinG: calorieTarget * macros.ProteinPercentage / 100f / 4f,
            carbG: calorieTarget * macros.CarbPercentage / 100f / 4f,
            fatG: calorieTarget * macros.FatPercentage / 100f / 9f
        );
    }

    public static MacroDistribution GetDefaultMacros(DietType dietType) => dietType switch
    {
        DietType.Balanced => new MacroDistribution(20, 50, 30),
        DietType.HighProtein => new MacroDistribution(30, 40, 30),
        DietType.Keto => new MacroDistribution(25, 5, 70),
        _ => throw new ArgumentException($"No default macros defined for this diet type. Received: {dietType}", nameof(dietType))
    };

}
