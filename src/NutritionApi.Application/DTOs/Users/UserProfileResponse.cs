using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.Users;

public record UserProfileResponse(
    Guid Id,
    DateOnly BirthDate,
    Gender Gender,
    ActivityLevel ActivityLevel,
    float Height,
    List<Allergen> Allergies,
    List<string> DietaryPreferences,
    SubscriptionTier SubscriptionTier,
    DateTime CreatedAt
)
{
    public static UserProfileResponse From(User user)
        => new(user.Id,
            user.BirthDate,
            user.Gender,
            user.ActivityLevel,
            user.Height,
            user.Allergies,
            user.DietaryPreferences,
            user.SubscriptionTier,
            user.CreatedAt);
}


