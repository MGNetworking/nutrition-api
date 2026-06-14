using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Entity;

public class DietPlan
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string Name { get; private set; } = string.Empty;
    public bool IsTemplate { get; private set; }
    public DietType DietType { get; private set; } = DietType.Unknown;
    public Goal Goal { get; private set; } = Goal.Unknown;  
    public float TargetWeight { get; private set; }
    public MacroDistribution MacroDistribution { get; private set; } = null!;

    // réservé à EF Core uniquement
    private DietPlan() { } 

    public DietPlan(Guid? userId,
        string name,
        bool isTemplate,
        DietType dietType,
        Goal goal,
        float targetWeight,
        MacroDistribution macroDistribution)
    {
        Id = Guid.NewGuid();
        if (!isTemplate && (userId == null || userId == Guid.Empty))
            throw new ArgumentException("User ID is required for a personal DietPlan.", nameof(userId));
        if (isTemplate && userId != null)
            throw new ArgumentException("A template DietPlan cannot be attached to a user.", nameof(userId));

        if (isTemplate)
            MarkAsTemplate();
        else
            UnmarkAsTemplate();

        UserId = userId;
        Rename(name);
        ChangeDietType(dietType);
        ChangeGoal(goal);

        if (targetWeight > 0f)
            SetTargetWeight(targetWeight);
        else 
            this.TargetWeight = targetWeight;

        AdjustMacros(macroDistribution);
    }

    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.Name = name;
    }

    public void MarkAsTemplate()
    {
        this.IsTemplate = true;
    }
    public void UnmarkAsTemplate()
    {
        this.IsTemplate = false;
    }


    public void ChangeDietType(DietType dietType)
    {
        if (dietType == DietType.Unknown)
            throw new ArgumentException($"Diet type must be defined. Received: {dietType}", nameof(dietType));
        this.DietType = dietType;
    }

    public void ChangeGoal(Goal goal)
    {
        if (goal == Goal.Unknown)
            throw new ArgumentException($"Goal must be defined. Received: {goal}", nameof(goal));
        this.Goal = goal;
    }

    public void SetTargetWeight(float targetWeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetWeight);
        this.TargetWeight = targetWeight;
    }

    public void AdjustMacros(MacroDistribution macroDistribution)
    {
        ArgumentNullException.ThrowIfNull(macroDistribution);
        this.MacroDistribution = macroDistribution;
    }
}
