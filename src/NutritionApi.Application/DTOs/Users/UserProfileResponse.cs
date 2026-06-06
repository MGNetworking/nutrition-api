using NutritionApi.Domain.Enums;

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
);



