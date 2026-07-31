using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Application.Tests.Nutrition;

[Trait("Level", "1")]
public class NutritionCalculatorFactoryTest
{
    private static readonly MacroDistribution DefaultMacros = new(20, 50, 30);

    // Mifflin male age=30 height=180 weight=80 → BMR=1780
    // Harris male age=30 height=180 weight=80 → BMR≈1853.6
    private static User MaleUser() => new(
        "keycloak-test",
        new DateOnly(DateTime.UtcNow.Year - 30, 1, 1),
        Gender.Male,
        ActivityLevel.ModeratelyActive,
        180f,
        [],
        []);

    [Fact]
    public void Create_Default_ReturnsMifflinCalculatorTest()
    {
        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(MaleUser(), 80f, Goal.Maintenance, DefaultMacros);

        Assert.Equal(1780f, needs.Bmr);
    }

    [Fact]
    public void Create_MifflinStJeor_ReturnsCorrectCalculatorTest()
    {
        var calculator = NutritionCalculatorFactory.Create(BmrFormula.MifflinStJeor);
        var needs = calculator.Calculate(MaleUser(), 80f, Goal.Maintenance, DefaultMacros);

        Assert.Equal(1780f, needs.Bmr);
    }

    [Fact]
    public void Create_HarrisBenedict_ReturnsCorrectCalculatorTest()
    {
        var calculator = NutritionCalculatorFactory.Create(BmrFormula.HarrisBenedict);
        var needs = calculator.Calculate(MaleUser(), 80f, Goal.Maintenance, DefaultMacros);

        Assert.Equal(1853.6, (double)needs.Bmr, 1);
    }

    [Fact]
    public void Create_MifflinAndHarris_ProduceDifferentBmrTest()
    {
        var mifflin = NutritionCalculatorFactory.Create(BmrFormula.MifflinStJeor);
        var harris = NutritionCalculatorFactory.Create(BmrFormula.HarrisBenedict);

        var needsMifflin = mifflin.Calculate(MaleUser(), 80f, Goal.Maintenance, DefaultMacros);
        var needsHarris = harris.Calculate(MaleUser(), 80f, Goal.Maintenance, DefaultMacros);

        Assert.NotEqual(needsMifflin.Bmr, needsHarris.Bmr);
    }

    [Fact]
    public void Create_UnknownFormula_ThrowsArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() =>
            NutritionCalculatorFactory.Create((BmrFormula)999));
    }
}
