using NutritionApi.Application.Exceptions;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Services;

/// <summary>Applique les restrictions liées au palier d'abonnement (Free, Pro, Business) sur les quotas et fonctionnalités.</summary>
public class SubscriptionGuard
{
    /// <summary>Vérifie que l'utilisateur n'a pas atteint sa limite de DietPlans personnels.</summary>
    /// <param name="tier">Palier d'abonnement de l'utilisateur.</param>
    /// <param name="currentCount">Nombre actuel de DietPlans personnels.</param>
    /// <exception cref="ForbiddenException">La limite est atteinte (Free: 2, Pro: 20, Business: illimité).</exception>
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

    /// <summary>Vérifie que l'utilisateur a accès aux templates partagés.</summary>
    /// <param name="tier">Palier d'abonnement de l'utilisateur.</param>
    /// <exception cref="ForbiddenException">Le tier est Free.</exception>
    public void CheckTemplateAccess(SubscriptionTier tier)
    {
        if (tier == SubscriptionTier.Free)
            throw new ForbiddenException("Templates are not accessible on the Free tier.");
    }

    /// <summary>Vérifie que l'utilisateur n'a pas atteint sa limite de repas sauvegardés.</summary>
    /// <param name="tier">Palier d'abonnement de l'utilisateur.</param>
    /// <param name="currentCount">Nombre actuel de repas sauvegardés.</param>
    /// <exception cref="ForbiddenException">La limite est atteinte (Free: 5, Pro: 50, Business: illimité).</exception>
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

    /// <summary>Vérifie que l'utilisateur n'a pas atteint sa limite de SavedFoodItems.</summary>
    /// <param name="tier">Palier d'abonnement de l'utilisateur.</param>
    /// <param name="currentCount">Nombre actuel de SavedFoodItems.</param>
    /// <exception cref="ForbiddenException">La limite est atteinte (Free: 10, Pro: 100, Business: illimité).</exception>
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

    /// <summary>Vérifie que la période demandée pour le bilan nutritionnel respecte la limite du tier.</summary>
    /// <param name="tier">Palier d'abonnement de l'utilisateur.</param>
    /// <param name="startDate">Date de début de la période du bilan.</param>
    /// <param name="endDate">Date de fin de la période du bilan.</param>
    /// <exception cref="ForbiddenException">La période dépasse la limite du tier (Free: 7 jours, Pro: 1 an, Business: illimité).</exception>
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
