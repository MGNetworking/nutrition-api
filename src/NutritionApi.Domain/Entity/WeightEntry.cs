namespace NutritionApi.Domain.Entity;

public class WeightEntry
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public float Weight { get; private set; }
    public DateOnly MeasuredAt { get; private set; }

    private WeightEntry() { }

    public WeightEntry(Guid userId, float weight, DateOnly measuredAt)
    {
        Id = Guid.NewGuid();

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        UserId = userId;
        Update(weight, measuredAt);
    }

    public void Update(float weight, DateOnly measuredAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
        if (measuredAt == default)
            throw new ArgumentException($"Measured date must be defined. Received: {measuredAt}", nameof(measuredAt));
        Weight = weight;
        MeasuredAt = measuredAt;
    }

}
