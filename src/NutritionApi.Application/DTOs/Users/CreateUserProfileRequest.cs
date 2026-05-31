using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Users;

public record CreateUserProfileRequest(
        DateOnly birthDate,
        Gender gender,
        ActivityLevel activityLevel,
        float height,
        List<Allergen> allergies,
        List<string> dietaryPreferences,
        float weight);
