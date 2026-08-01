using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.DTOS.Users;

/// <summary>Requête de création du profil utilisateur à la première connexion Keycloak.</summary>
/// <param name="birthDate">Date de naissance de l'utilisateur.</param>
/// <param name="gender">Genre de l'utilisateur (utilisé pour le calcul du BMR).</param>
/// <param name="activityLevel">Niveau d'activité physique (utilisé pour le calcul du TDEE).</param>
/// <param name="height">Taille de l'utilisateur, en centimètres.</param>
/// <param name="allergies">Allergènes de l'utilisateur.</param>
/// <param name="dietaryPreferences">Régimes alimentaires déclarés, en liste fermée.</param>
/// <param name="weight">Poids initial, en kilogrammes — crée la première pesée.</param>
public record CreateUserProfileRequest(
        DateOnly birthDate,
        Gender gender,
        ActivityLevel activityLevel,
        float height,
        List<Allergen> allergies,
        List<DietaryPreference> dietaryPreferences,
        float weight);
