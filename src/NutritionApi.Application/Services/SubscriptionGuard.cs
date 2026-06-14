using NutritionApi.Application.Exceptions;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Services;

public class SubscriptionGuard
{
    /// <summary>Vérifie que l'utilisateur n'a pas atteint sa limite de DietPlans personnels. Lève <see cref="ForbiddenException"/> si la limite est atteinte (Free: 2, Pro: 20, Business: illimité).</summary>
    public void CheckDietPlanLimit(SubscriptionTier tier, int currentCount)
    {
        var limit = tier switch
        {
            SubscriptionTier.Free => 2,
            SubscriptionTier.Pro => 20,
            SubscriptionTier.Business => int.MaxValue,
            _ => throw new ArgumentException($"Unknown tier. Received: {tier}", nameof(tier))
        };

        if (currentCount >= limit)
            throw new ForbiddenException($"DietPlan limit reached for tier {tier} (max {limit}).");
    }

    /// <summary>Vérifie que l'utilisateur a accès aux templates partagés. Lève <see cref="ForbiddenException"/> si le tier est Free.</summary>
    public void CheckTemplateAccess(SubscriptionTier tier)
    {
        if (tier == SubscriptionTier.Free)
            throw new ForbiddenException("Templates are not accessible on the Free tier.");
    }

    /// <summary>Vérifie que l'utilisateur n'a pas atteint sa limite de repas sauvegardés. Lève <see cref="ForbiddenException"/> si la limite est atteinte (Free: 5, Pro: 50, Business: illimité).</summary>
    public void CheckSavedMealLimit(SubscriptionTier tier, int currentCount)
    {
        var limit = tier switch
        {
            SubscriptionTier.Free => 5,
            SubscriptionTier.Pro => 50,
            SubscriptionTier.Business => int.MaxValue,
            _ => throw new ArgumentException($"Unknown tier. Received: {tier}", nameof(tier))
        };

        if (currentCount >= limit)
            throw new ForbiddenException($"Saved meal limit reached for tier {tier} (max {limit}).");
    }

    /// <summary>Vérifie que l'utilisateur n'a pas atteint sa limite de SavedFoodItems. Lève <see cref="ForbiddenException"/> si la limite est atteinte (Free: 10, Pro: 100, Business: illimité).</summary>
    public void CheckSavedFoodItemLimit(SubscriptionTier tier, int currentCount)
    {
        var limit = tier switch
        {
            SubscriptionTier.Free => 10,
            SubscriptionTier.Pro => 100,
            SubscriptionTier.Business => int.MaxValue,
            _ => throw new ArgumentException($"Unknown tier. Received: {tier}", nameof(tier))
        };

        if (currentCount >= limit)
            throw new ForbiddenException($"SavedFoodItem limit reached for tier {tier} (max {limit}).");
    }

    /// <summary>Vérifie que la période demandée pour le bilan nutritionnel respecte la limite du tier. Lève <see cref="ForbiddenException"/> si dépassée (Free: 7 jours, Pro: 1 an, Business: illimité).</summary>
    public void CheckBilanPeriod(SubscriptionTier tier, DateOnly startDate, DateOnly endDate)
    {
        var days = endDate.DayNumber - startDate.DayNumber;

        var maxDays = tier switch
        {
            SubscriptionTier.Free => 7,
            SubscriptionTier.Pro => 365,
            SubscriptionTier.Business => int.MaxValue,
            _ => throw new ArgumentException($"Unknown tier. Received: {tier}", nameof(tier))
        };

        if (days > maxDays)
            throw new ForbiddenException($"Bilan period exceeds the limit for tier {tier} (max {maxDays} days).");
    }
}
