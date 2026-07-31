using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Tests.Nutrition;

[Trait("Level", "1")]
public class HarrisBenedictStrategyTest
{
    private readonly HarrisBenedictStrategy _strategy = new();

    private static User MaleUser() => new(
        "keycloak-test",
        new DateOnly(DateTime.UtcNow.Year - 30, 1, 1),
        Gender.Male,
        ActivityLevel.ModeratelyActive,
        180f,
        [],
        []);

    private static User FemaleUser() => new(
        "keycloak-test",
        new DateOnly(DateTime.UtcNow.Year - 30, 1, 1),
        Gender.Female,
        ActivityLevel.ModeratelyActive,
        165f,
        [],
        []);

    // Formule homme : 88.362 + 13.397*P + 4.799*T - 5.677*A
    // age=30, weight=80, height=180 → 88.362 + 1071.76 + 863.82 - 170.31 = 1853.632
    [Fact]
    public void Calculate_Male_ReturnsExpectedBmrTest()
    {
        var result = _strategy.Calculate(MaleUser(), 80f);

        Assert.Equal(1853.6, (double)result, 1);
    }

    // Formule femme : 447.593 + 9.247*P + 3.098*T - 4.330*A
    // age=30, weight=80, height=165 → 447.593 + 739.76 + 511.17 - 129.9 = 1568.623
    [Fact]
    public void Calculate_Female_ReturnsExpectedBmrTest()
    {
        var result = _strategy.Calculate(FemaleUser(), 80f);

        Assert.Equal(1568.6, (double)result, 1);
    }

    [Fact]
    public void Calculate_HigherWeight_ReturnsHigherBmrTest()
    {
        var heavy = _strategy.Calculate(MaleUser(), 100f);
        var light = _strategy.Calculate(MaleUser(), 60f);

        Assert.True(heavy > light);
    }

    [Fact]
    public void Calculate_Male_IsHigherThanFemale_SameParametersTest()
    {
        var male = _strategy.Calculate(MaleUser(), 80f);
        var female = _strategy.Calculate(FemaleUser(), 80f);

        Assert.True(male > female);
    }

    [Fact]
    public void Calculate_ProducesHigherBmrThanMifflin_MaleTest()
    {
        var harris = new HarrisBenedictStrategy();
        var mifflin = new MifflinStJeorStrategy();

        var harrisBmr = harris.Calculate(MaleUser(), 80f);
        var mifflinBmr = mifflin.Calculate(MaleUser(), 80f);

        Assert.True(harrisBmr > mifflinBmr);
    }
}
