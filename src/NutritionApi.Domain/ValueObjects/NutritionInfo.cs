namespace NutritionApi.Domain.ValueObjects;

public sealed record NutritionInfo
{
    public float Calories { get; }
    public int Proteins { get; }
    public int Carbs { get; }
    public int Fats { get; }

    public NutritionInfo(float calories, int proteins, int carbs, int fats)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(calories);
        ArgumentOutOfRangeException.ThrowIfNegative(proteins);
        ArgumentOutOfRangeException.ThrowIfNegative(carbs);
        ArgumentOutOfRangeException.ThrowIfNegative(fats);

        Calories = calories;
        Proteins = proteins;
        Carbs = carbs;
        Fats = fats;
    }
}
