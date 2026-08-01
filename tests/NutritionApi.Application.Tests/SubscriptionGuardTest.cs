namespace NutritionApi.Application.Tests;

using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Services;
using NutritionApi.Domain.Enums;

[Trait("Level", "1")]
public class SubscriptionGuardTest
{
    private readonly SubscriptionGuard _subscriptionGuard = new();

    // CheckDietPlanLimit
    [Fact]
    public void CheckDietPlanLimit_ShouldNotThrow_WhenUnderLimit()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckDietPlanLimit(SubscriptionTier.Free, 1));
        Assert.Null(exception);
    }

    [Fact]
    public void CheckDietPlanLimit_ShouldThrow_WhenFreeTierReachesLimit()
    {
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckDietPlanLimit(SubscriptionTier.Free, 2));
    }

    [Fact]
    public void CheckDietPlanLimit_ShouldThrow_WhenProTierReachesLimit()
    {
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckDietPlanLimit(SubscriptionTier.Pro, 20));
    }

    [Fact]
    public void CheckDietPlanLimit_ShouldNotThrow_WhenBusinessTier()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckDietPlanLimit(SubscriptionTier.Business, 1000));
        Assert.Null(exception);
    }

    // CheckTemplateAccess
    [Fact]
    public void CheckTemplateAccess_ShouldThrow_WhenFreeTier()
    {
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckTemplateAccess(SubscriptionTier.Free));
    }

    [Fact]
    public void CheckTemplateAccess_ShouldNotThrow_WhenProTier()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckTemplateAccess(SubscriptionTier.Pro));
        Assert.Null(exception);
    }

    [Fact]
    public void CheckTemplateAccess_ShouldNotThrow_WhenBusinessTier()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckTemplateAccess(SubscriptionTier.Business));
        Assert.Null(exception);
    }

    // CheckSavedMealLimit
    [Fact]
    public void CheckSavedMealLimit_ShouldNotThrow_WhenUnderLimit()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckSavedMealLimit(SubscriptionTier.Free, 4));
        Assert.Null(exception);
    }

    [Fact]
    public void CheckSavedMealLimit_ShouldThrow_WhenFreeTierReachesLimit()
    {
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckSavedMealLimit(SubscriptionTier.Free, 5));
    }

    [Fact]
    public void CheckSavedMealLimit_ShouldThrow_WhenProTierReachesLimit()
    {
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckSavedMealLimit(SubscriptionTier.Pro, 50));
    }

    [Fact]
    public void CheckSavedMealLimit_ShouldNotThrow_WhenBusinessTier()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckSavedMealLimit(SubscriptionTier.Business, 1000));
        Assert.Null(exception);
    }

    // CheckSavedFoodItemLimit
    [Fact]
    public void CheckSavedFoodItemLimit_ShouldNotThrow_WhenUnderLimit()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckSavedFoodItemLimit(SubscriptionTier.Free, 9));
        Assert.Null(exception);
    }

    [Fact]
    public void CheckSavedFoodItemLimit_ShouldThrow_WhenFreeTierReachesLimit()
    {
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckSavedFoodItemLimit(SubscriptionTier.Free, 10));
    }

    [Fact]
    public void CheckSavedFoodItemLimit_ShouldThrow_WhenProTierReachesLimit()
    {
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckSavedFoodItemLimit(SubscriptionTier.Pro, 100));
    }

    [Fact]
    public void CheckSavedFoodItemLimit_ShouldNotThrow_WhenBusinessTier()
    {
        var exception = Record.Exception(() => _subscriptionGuard.CheckSavedFoodItemLimit(SubscriptionTier.Business, 1000));
        Assert.Null(exception);
    }

    // CheckBilanPeriod
    [Fact]
    public void CheckBilanPeriod_ShouldNotThrow_WhenUnderLimit()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(6);
        var exception = Record.Exception(() => _subscriptionGuard.CheckBilanPeriod(SubscriptionTier.Free, start, end));
        Assert.Null(exception);
    }

    [Fact]
    public void CheckBilanPeriod_ShouldThrow_WhenFreeTierExceedsLimit()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(8);
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckBilanPeriod(SubscriptionTier.Free, start, end));
    }

    [Fact]
    public void CheckBilanPeriod_ShouldThrow_WhenProTierExceedsLimit()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(366);
        Assert.Throws<ForbiddenException>(() => _subscriptionGuard.CheckBilanPeriod(SubscriptionTier.Pro, start, end));
    }

    [Fact]
    public void CheckBilanPeriod_ShouldNotThrow_WhenBusinessTier()
    {
        var start = DateOnly.FromDateTime(DateTime.Today);
        var end = start.AddDays(3650);
        var exception = Record.Exception(() => _subscriptionGuard.CheckBilanPeriod(SubscriptionTier.Business, start, end));
        Assert.Null(exception);
    }
}
