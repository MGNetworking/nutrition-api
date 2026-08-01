
namespace NutritionApi.Domain.Tests;

using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

[Trait("Level", "1")]
public class DietcsTest
{
    static int ProteinPercentage = 20;
    static int CarbPercentage = 50;
    static int FatPercentage = 30;
    static Diet creatDietHelper(
            Guid? userId = null,
            string name = null!,
            DietType? dietType = null,
            Goal? goal = null,
            float targetWeight = 70.0f,
            int calorieTarget = 2000,
            MacroDistribution? macroDistribution = null)
    {
        return new Diet(
             userId: userId ?? Guid.NewGuid(),
             name: name ?? "Diet",
             dietType: dietType ?? DietType.Balanced,
             goal: goal ?? Goal.Maintenance,
             targetWeight: targetWeight,
             calorieTarget: calorieTarget,
             macroDistribution: macroDistribution ?? new MacroDistribution(ProteinPercentage, CarbPercentage, FatPercentage));

    }

    [Fact]
    public void ChangeDietStatus_ThrowArgumentExceptionTest()
    {
        Diet dt = creatDietHelper();
        Assert.Throws<ArgumentException>(() => dt.ChangeDietStatus(DietStatus.Unknown));
    }

    [Fact]
    public void ChangeDietStatus_ThrowInvalidOperationExceptionTest()
    {
        Diet dt = creatDietHelper();
        dt.ChangeDietStatus(DietStatus.Archived);
        Assert.Throws<InvalidOperationException>(() => dt.ChangeDietStatus(DietStatus.Cancelled));
    }

    [Fact]
    public void ChangeDietStatus_Archived_SetsEndDateTest()
    {
        Diet dt = creatDietHelper();
        dt.ChangeDietStatus(DietStatus.Archived);
        Assert.NotNull(dt.EndDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructeurName_ArgumentException(string? name)
    {
        Assert.Throws<ArgumentException>(() => creatDietHelper(name: name));
    }

    [Fact]
    public void Constructor_Name_ThrowIfNullTest()
    {
        Assert.Throws<ArgumentNullException>(() => new Diet(
            userId: Guid.NewGuid(),
            name: null!,
            dietType: DietType.Balanced,
            goal: Goal.Maintenance,
            targetWeight: 70.0f,
            calorieTarget: 2000,
            macroDistribution: new MacroDistribution(20, 50, 30)));
    }

    [Fact]
    public void Constructor_UserId_ThrowArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => creatDietHelper(userId: Guid.Empty));
    }

    [Fact]
    public void Constructor_DietType_ThrowArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => creatDietHelper(dietType: DietType.Unknown));
    }

    [Fact]
    public void Constructor_Goal_ThrowArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => creatDietHelper(goal: Goal.Unknown));
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void Constructor_TargetWeight_ThrowArgumentOutOfRangeExceptionTest(float weight)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => creatDietHelper(targetWeight: weight));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_CalorieTarget_ThrowArgumentOutOfRangeExceptionTest(int calories)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => creatDietHelper(calorieTarget: calories));
    }

    [Fact]
    public void Constructor_MacroDistribution_ThrowArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new Diet(
            userId: Guid.NewGuid(),
            name: "Diet",
            dietType: DietType.Balanced,
            goal: Goal.Maintenance,
            targetWeight: 70.0f,
            calorieTarget: 2000,
            macroDistribution: null!));
    }

    [Fact]
    public void Constructor_OkTest()
    {
        Diet dt = creatDietHelper();
        Assert.Equal(DietStatus.Active, dt.StatusDiet);
        Assert.NotEqual(Guid.Empty, dt.UserId);
        Assert.Null(dt.EndDate);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow), dt.StartDate);
    }
}
