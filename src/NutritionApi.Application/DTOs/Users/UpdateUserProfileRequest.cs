namespace NutritionApi.Application.DTOS.Users;
using NutritionApi.Domain.Enums;

public record UpdateUserProfileRequest(
    DateOnly BirthDate,
    Gender Gender,
    ActivityLevel ActivityLevel,
    float Height,
    List<Allergen> Allergies,
    List<string> DietaryPreferences
);