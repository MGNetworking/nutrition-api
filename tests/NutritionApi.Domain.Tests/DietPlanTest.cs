using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Tests;

public class DietPlanTest
{
    static MacroDistribution DefaultMacros() => new(20, 50, 30);

    static DietPlan CreatePersonalPlan(
        Guid? userId = null,
        string name = "Mon plan",
        DietType dietType = DietType.Balanced,
        Goal goal = Goal.Maintenance,
        float targetWeight = 75.0f,
        MacroDistribution? macros = null)
    {
        return new DietPlan(
            userId: userId ?? Guid.NewGuid(),
            name: name,
            isTemplate: false,
            dietType: dietType,
            goal: goal,
            targetWeight: targetWeight,
            macroDistribution: macros ?? DefaultMacros());
    }

    static DietPlan CreateTemplatePlan(
        string name = "Template plan",
        DietType dietType = DietType.Balanced,
        Goal goal = Goal.Maintenance,
        float targetWeight = 75.0f,
        MacroDistribution? macros = null)
    {
        return new DietPlan(
            userId: null,
            name: name,
            isTemplate: true,
            dietType: dietType,
            goal: goal,
            targetWeight: targetWeight,
            macroDistribution: macros ?? DefaultMacros());
    }

    // --- Constructeur : plan personnel ---

    [Fact]
    public void Constructor_Personal_OkTest()
    {
        Guid userId = Guid.NewGuid();
        DietPlan plan = CreatePersonalPlan(userId: userId);

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Equal(userId, plan.UserId);
        Assert.False(plan.IsTemplate);
        Assert.Equal("Mon plan", plan.Name);
        Assert.Equal(DietType.Balanced, plan.DietType);
        Assert.Equal(Goal.Maintenance, plan.Goal);
        Assert.Equal(75.0f, plan.TargetWeight);
        Assert.NotNull(plan.MacroDistribution);
    }

    [Fact]
    public void Constructor_Personal_UserId_Null_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new DietPlan(
            userId: null,
            name: "Mon plan",
            isTemplate: false,
            dietType: DietType.Balanced,
            goal: Goal.Maintenance,
            targetWeight: 75.0f,
            macroDistribution: DefaultMacros()));
    }

    [Fact]
    public void Constructor_Personal_UserId_Empty_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreatePersonalPlan(userId: Guid.Empty));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_Personal_Name_WhiteSpace_ThrowsArgumentExceptionTest(string name)
    {
        Assert.Throws<ArgumentException>(() => CreatePersonalPlan(name: name));
    }

    [Fact]
    public void Constructor_Personal_Name_Null_ThrowsArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new DietPlan(
            userId: Guid.NewGuid(),
            name: null!,
            isTemplate: false,
            dietType: DietType.Balanced,
            goal: Goal.Maintenance,
            targetWeight: 75.0f,
            macroDistribution: DefaultMacros()));
    }

    [Fact]
    public void Constructor_Personal_DietType_Unknown_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreatePersonalPlan(dietType: DietType.Unknown));
    }

    [Fact]
    public void Constructor_Personal_Goal_Unknown_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreatePersonalPlan(goal: Goal.Unknown));
    }


    [Fact]
    public void Constructor_Personal_Macros_Null_ThrowsArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => new DietPlan(
            userId: Guid.NewGuid(),
            name: "Mon plan",
            isTemplate: false,
            dietType: DietType.Balanced,
            goal: Goal.Maintenance,
            targetWeight: 75.0f,
            macroDistribution: null!));
    }

    // --- Constructeur : template ---

    [Fact]
    public void Constructor_Template_OkTest()
    {
        DietPlan plan = CreateTemplatePlan();

        Assert.NotEqual(Guid.Empty, plan.Id);
        Assert.Null(plan.UserId);
        Assert.True(plan.IsTemplate);
    }

    [Fact]
    public void Constructor_Template_WithUserId_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => new DietPlan(
            userId: Guid.NewGuid(),
            name: "Template",
            isTemplate: true,
            dietType: DietType.Balanced,
            goal: Goal.Maintenance,
            targetWeight: 75.0f,
            macroDistribution: DefaultMacros()));
    }

    // --- Rename ---

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_WhiteSpace_ThrowsArgumentExceptionTest(string name)
    {
        DietPlan plan = CreatePersonalPlan();
        Assert.Throws<ArgumentException>(() => plan.Rename(name));
    }

    [Fact]
    public void Rename_Null_ThrowsArgumentNullExceptionTest()
    {
        DietPlan plan = CreatePersonalPlan();
        Assert.Throws<ArgumentNullException>(() => plan.Rename(null!));
    }

    [Fact]
    public void Rename_OkTest()
    {
        DietPlan plan = CreatePersonalPlan();
        plan.Rename("Nouveau nom");
        Assert.Equal("Nouveau nom", plan.Name);
    }

    // --- ChangeDietType ---

    [Fact]
    public void ChangeDietType_Unknown_ThrowsArgumentExceptionTest()
    {
        DietPlan plan = CreatePersonalPlan();
        Assert.Throws<ArgumentException>(() => plan.ChangeDietType(DietType.Unknown));
    }

    [Fact]
    public void ChangeDietType_OkTest()
    {
        DietPlan plan = CreatePersonalPlan();
        plan.ChangeDietType(DietType.Keto);
        Assert.Equal(DietType.Keto, plan.DietType);
    }

    // --- ChangeGoal ---

    [Fact]
    public void ChangeGoal_Unknown_ThrowsArgumentExceptionTest()
    {
        DietPlan plan = CreatePersonalPlan();
        Assert.Throws<ArgumentException>(() => plan.ChangeGoal(Goal.Unknown));
    }

    [Fact]
    public void ChangeGoal_OkTest()
    {
        DietPlan plan = CreatePersonalPlan();
        plan.ChangeGoal(Goal.WeightLoss);
        Assert.Equal(Goal.WeightLoss, plan.Goal);
    }

    // --- SetTargetWeight ---

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-5.0f)]
    public void SetTargetWeight_Invalid_ThrowsArgumentOutOfRangeExceptionTest(float weight)
    {
        DietPlan plan = CreatePersonalPlan();
        Assert.Throws<ArgumentOutOfRangeException>(() => plan.SetTargetWeight(weight));
    }

    [Fact]
    public void SetTargetWeight_OkTest()
    {
        DietPlan plan = CreatePersonalPlan();
        plan.SetTargetWeight(80.0f);
        Assert.Equal(80.0f, plan.TargetWeight);
    }

    // --- AdjustMacros ---

    [Fact]
    public void AdjustMacros_Null_ThrowsArgumentNullExceptionTest()
    {
        DietPlan plan = CreatePersonalPlan();
        Assert.Throws<ArgumentNullException>(() => plan.AdjustMacros(null!));
    }

    [Fact]
    public void AdjustMacros_OkTest()
    {
        DietPlan plan = CreatePersonalPlan();
        MacroDistribution newMacros = new(30, 40, 30);
        plan.AdjustMacros(newMacros);
        Assert.Equal(newMacros, plan.MacroDistribution);
    }

    // --- MarkAsTemplate / UnmarkAsTemplate ---

    [Fact]
    public void MarkAsTemplate_SetsIsTemplateTrue_OkTest()
    {
        DietPlan plan = CreatePersonalPlan();
        plan.MarkAsTemplate();
        Assert.True(plan.IsTemplate);
    }

    [Fact]
    public void UnmarkAsTemplate_SetsIsTemplateFalse_OkTest()
    {
        DietPlan plan = CreateTemplatePlan();
        plan.UnmarkAsTemplate();
        Assert.False(plan.IsTemplate);
    }
}
