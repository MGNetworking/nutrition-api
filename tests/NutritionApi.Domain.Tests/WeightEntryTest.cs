using NutritionApi.Domain.Entity;

namespace NutritionApi.Domain.Tests;

public class WeightEntryTest
{
    static WeightEntry CreateWeightEntry(
        Guid? userId = null,
        float weight = 75.0f,
        DateOnly? measuredAt = null)
    {
        return new WeightEntry(
            userId: userId ?? Guid.NewGuid(),
            weight: weight,
            measuredAt: measuredAt ?? DateOnly.FromDateTime(DateTime.UtcNow));
    }

    // --- Constructeur ---

    [Fact]
    public void Constructor_OkTest()
    {
        Guid userId = Guid.NewGuid();
        DateOnly measuredAt = DateOnly.FromDateTime(DateTime.UtcNow);

        WeightEntry entry = new WeightEntry(userId, 75.0f, measuredAt);

        Assert.NotEqual(Guid.Empty, entry.Id);
        Assert.Equal(userId, entry.UserId);
        Assert.Equal(75.0f, entry.Weight);
        Assert.Equal(measuredAt, entry.MeasuredAt);
    }

    [Fact]
    public void Constructor_UserId_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateWeightEntry(userId: Guid.Empty));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void Constructor_Weight_Invalid_ThrowsArgumentOutOfRangeExceptionTest(float weight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateWeightEntry(weight: weight));
    }

    [Fact]
    public void Constructor_MeasuredAt_Default_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new WeightEntry(
            userId: Guid.NewGuid(),
            weight: 75.0f,
            measuredAt: default));
    }

    // --- Update ---

    [Fact]
    public void Update_OkTest()
    {
        WeightEntry entry = CreateWeightEntry();
        DateOnly newDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

        entry.Update(80.0f, newDate);

        Assert.Equal(80.0f, entry.Weight);
        Assert.Equal(newDate, entry.MeasuredAt);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void Update_Weight_Invalid_ThrowsArgumentOutOfRangeExceptionTest(float weight)
    {
        WeightEntry entry = CreateWeightEntry();
        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(weight, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void Update_MeasuredAt_Default_ThrowsArgumentExceptionTest()
    {
        WeightEntry entry = CreateWeightEntry();
        Assert.Throws<ArgumentException>(() => entry.Update(75.0f, default));
    }
}
