using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Application.Tests.Nutrition;

public class NutritionCalculatorTest
{
    private static readonly MacroDistribution DefaultMacros = new(20, 50, 30);

    // Mifflin male age=30 height=180 weight=80 → BMR=1780
    private static User MaleUser(ActivityLevel activity) => new(
        "keycloak-test",
        new DateOnly(DateTime.UtcNow.Year - 30, 1, 1),
        Gender.Male,
        activity,
        180f,
        [],
        []);

    // --- Calculate — BMR ---

    [Fact]
    public void Calculate_ReturnsBmrInNutritionNeedsTest()
    {
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(ActivityLevel.ModeratelyActive), 80f, Goal.Maintenance, DefaultMacros);

        Assert.Equal(1780f, needs.Bmr);
    }

    // --- Calculate — TDEE par ActivityLevel ---
    // BMR=1780 × NAP attendu

    [Theory]
    [InlineData(ActivityLevel.Sedentary, 2136f)]
    [InlineData(ActivityLevel.LightlyActive, 2447.5f)]
    [InlineData(ActivityLevel.ModeratelyActive, 2759f)]
    [InlineData(ActivityLevel.VeryActive, 3070.5f)]
    [InlineData(ActivityLevel.ExtremelyActive, 3382f)]
    public void Calculate_Tdee_ByActivityLevel_ReturnsExpectedValueTest(ActivityLevel activity, float expectedTdee)
    {
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(activity), 80f, Goal.Maintenance, DefaultMacros);

        Assert.Equal(expectedTdee, needs.Tdee);
    }

    // --- Calculate — CalorieTarget par Goal ---

    [Fact]
    public void Calculate_Maintenance_TargetEqualsTdeeTest()
    {
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(ActivityLevel.ModeratelyActive), 80f, Goal.Maintenance, DefaultMacros);

        Assert.Equal(needs.Tdee, needs.TargetCalories);
    }

    [Fact]
    public void Calculate_WeightLoss_TargetIsBelowTdeeTest()
    {
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(ActivityLevel.ModeratelyActive), 80f, Goal.WeightLoss, DefaultMacros);

        Assert.True(needs.TargetCalories < needs.Tdee);
    }

    [Fact]
    public void Calculate_WeightGain_TargetIsAboveTdeeTest()
    {
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(ActivityLevel.ModeratelyActive), 80f, Goal.WeightGain, DefaultMacros);

        Assert.True(needs.TargetCalories > needs.Tdee);
    }

    [Fact]
    public void Calculate_WeightLossDeficit_CappedAt500KcalTest()
    {
        // Utiliser un TDEE très élevé pour déclencher le plafond à 500 kcal
        // ExtremelyActive → TDEE = 1780 * 1.9 = 3382 → 15% = 507.3 > 500 → plafonné à 500
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(ActivityLevel.ExtremelyActive), 80f, Goal.WeightLoss, DefaultMacros);

        Assert.Equal(needs.Tdee - 500f, needs.TargetCalories);
    }

    [Fact]
    public void Calculate_WeightGainSurplus_CappedAt500KcalTest()
    {
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(ActivityLevel.ExtremelyActive), 80f, Goal.WeightGain, DefaultMacros);

        Assert.Equal(needs.Tdee + 500f, needs.TargetCalories);
    }

    [Fact]
    public void Calculate_ReturnsMacroDistributionUnchangedTest()
    {
        var macros = new MacroDistribution(30, 40, 30);
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(ActivityLevel.ModeratelyActive), 80f, Goal.Maintenance, macros);

        Assert.Equal(macros, needs.MacroDistribution);
    }

    // --- ToGrams ---

    [Fact]
    public void ToGrams_ReturnsExpectedGramsTest()
    {
        // 20/50/30 sur 2000 kcal : P=100g, C=250g, F=66.7g (2000*0.30/9=66.666...)
        var result = NutritionCalculator.ToGrams(new MacroDistribution(20, 50, 30), 2000);

        Assert.Equal(100f, result.ProteinG);
        Assert.Equal(250f, result.CarbG);
        Assert.Equal(66.7, (double)result.FatG, 1);
    }

    [Fact]
    public void ToGrams_HighProtein_ReturnsExpectedGramsTest()
    {
        // 30/40/30 sur 2500 kcal : P=187.5g, C=250g, F=83.3g
        var result = NutritionCalculator.ToGrams(new MacroDistribution(30, 40, 30), 2500);

        Assert.Equal(187.5f, result.ProteinG);
        Assert.Equal(250f, result.CarbG);
        Assert.Equal(83.3, (double)result.FatG, 1);
    }

    [Fact]
    public void ToGrams_ZeroCalorieTarget_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionCalculator.ToGrams(new MacroDistribution(20, 50, 30), 0));
    }

    [Fact]
    public void ToGrams_NegativeCalorieTarget_ThrowsArgumentOutOfRangeExceptionTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            NutritionCalculator.ToGrams(new MacroDistribution(20, 50, 30), -100));
    }

    // --- GetDefaultMacros ---

    [Fact]
    public void GetDefaultMacros_Balanced_ReturnsExpectedTest()
    {
        var result = NutritionCalculator.GetDefaultMacros(DietType.Balanced);

        Assert.Equal(new MacroDistribution(20, 50, 30), result);
    }

    [Fact]
    public void GetDefaultMacros_HighProtein_ReturnsExpectedTest()
    {
        var result = NutritionCalculator.GetDefaultMacros(DietType.HighProtein);

        Assert.Equal(new MacroDistribution(30, 40, 30), result);
    }

    [Fact]
    public void GetDefaultMacros_Keto_ReturnsExpectedTest()
    {
        var result = NutritionCalculator.GetDefaultMacros(DietType.Keto);

        Assert.Equal(new MacroDistribution(25, 5, 70), result);
    }

    [Fact]
    public void GetDefaultMacros_UnknownDietType_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() =>
            NutritionCalculator.GetDefaultMacros((DietType)999));
    }
}
