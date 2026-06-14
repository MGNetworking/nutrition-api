using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Tests.Nutrition;

public class MifflinStJeorStrategyTest
{
    private readonly MifflinStJeorStrategy _strategy = new();

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

    // Formule homme : 10*P + 6.25*T - 5*A + 5
    // age=30, weight=80, height=180 → 800 + 1125 - 150 + 5 = 1780
    [Fact]
    public void Calculate_Male_ReturnsExpectedBmrTest()
    {
        var result = _strategy.Calculate(MaleUser(), 80f);

        Assert.Equal(1780f, result);
    }

    // Formule femme : 10*P + 6.25*T - 5*A - 161
    // age=30, weight=80, height=165 → 800 + 1031.25 - 150 - 161 = 1520.25
    [Fact]
    public void Calculate_Female_ReturnsExpectedBmrTest()
    {
        var result = _strategy.Calculate(FemaleUser(), 80f);

        Assert.Equal(1520.25f, result);
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
}
