using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Users;

/// <summary>Requête de mise à jour du profil utilisateur.</summary>
/// <param name="BirthDate">Date de naissance de l'utilisateur.</param>
/// <param name="Gender">Genre de l'utilisateur (utilisé pour le calcul du BMR).</param>
/// <param name="ActivityLevel">Niveau d'activité physique (utilisé pour le calcul du TDEE).</param>
/// <param name="Height">Taille de l'utilisateur, en centimètres.</param>
/// <param name="Allergies">Allergènes de l'utilisateur.</param>
/// <param name="DietaryPreferences">Préférences alimentaires libres.</param>
public record UpdateUserProfileRequest(
    DateOnly BirthDate,
    Gender Gender,
    ActivityLevel ActivityLevel,
    float Height,
    List<Allergen> Allergies,
    List<string> DietaryPreferences
);