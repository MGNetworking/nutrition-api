namespace NutritionApi.Domain.Entity;

using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

public class Diet
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Name { get; private set; } = string.Empty;
    public DietType DietType { get; private set; }
    public Goal Goal { get; private set; }
    public float TargetWeight { get; private set; }
    public int CalorieTarget { get; private set; }
    public MacroDistribution MacroDistribution { get; private set; } = null!;
    public DietStatus StatusDiet { get; private set; } = DietStatus.Unknown;
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    // Pour EF Core uniquement
    private Diet() { }

    public Diet(Guid userId,
        string name,
        DietType dietType,
        Goal goal,
        float targetWeight,
        int calorieTarget,
        MacroDistribution macroDistribution)
    {
        Id = Guid.NewGuid();

        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Received: {userId}", nameof(userId));
        UserId = userId;

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;

        if (dietType == DietType.Unknown)
            throw new ArgumentException($"Diet type must be defined. Received: {dietType}", nameof(dietType));
        DietType = dietType;

        if (goal == Goal.Unknown)
            throw new ArgumentException($"Goal must be defined. Received: {goal}", nameof(goal));
        Goal = goal;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetWeight);
        TargetWeight = targetWeight;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(calorieTarget);
        CalorieTarget = calorieTarget;

        ArgumentNullException.ThrowIfNull(macroDistribution);
        MacroDistribution = macroDistribution;

        StatusDiet = DietStatus.Active;
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public void ChangeDietStatus(DietStatus dietStatus)
    {
        if (dietStatus == DietStatus.Unknown)
            throw new ArgumentException($"Diet status cannot be Unknown. Received: {dietStatus}", nameof(dietStatus));
        if (StatusDiet == DietStatus.Archived)
            throw new InvalidOperationException("An archived diet cannot be modified.");
        if (dietStatus == DietStatus.Archived || dietStatus == DietStatus.Cancelled)
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow);

        StatusDiet = dietStatus;
    }
}
