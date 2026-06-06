using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Users;

public record UpdateUserProfileRequest(
    DateOnly BirthDate,
    Gender Gender,
    ActivityLevel ActivityLevel,
    float Height,
    List<Allergen> Allergies,
    List<string> DietaryPreferences
);