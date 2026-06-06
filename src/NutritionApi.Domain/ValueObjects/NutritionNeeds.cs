namespace NutritionApi.Domain.ValueObjects;

public sealed record NutritionNeeds
{
    public float Bmr { get; }
    public float Tdee { get; }
    public float TargetCalories { get; }
    public MacroDistribution MacroDistribution { get; }

    public NutritionNeeds(float bmr, float tdee, float targetCalories, MacroDistribution macroDistribution)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bmr);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tdee);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetCalories);
        ArgumentNullException.ThrowIfNull(macroDistribution);

        Bmr = bmr;
        Tdee = tdee;
        TargetCalories = targetCalories;
        MacroDistribution = macroDistribution;
    }
}
