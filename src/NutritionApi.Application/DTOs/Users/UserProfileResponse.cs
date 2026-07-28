using NutritionApi.Domain.Enums;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOS.Users;

/// <summary>Profil de l'utilisateur connecté.</summary>
/// <param name="Id">Identifiant interne de l'utilisateur.</param>
/// <param name="BirthDate">Date de naissance de l'utilisateur.</param>
/// <param name="Gender">Genre de l'utilisateur.</param>
/// <param name="ActivityLevel">Niveau d'activité physique.</param>
/// <param name="Height">Taille de l'utilisateur, en centimètres.</param>
/// <param name="Allergies">Allergènes de l'utilisateur.</param>
/// <param name="DietaryPreferences">Régimes alimentaires déclarés, en liste fermée.</param>
/// <param name="SubscriptionTier">Palier d'abonnement (Free, Pro ou Business).</param>
/// <param name="CreatedAt">Date de création du compte (UTC).</param>
public record UserProfileResponse(
    Guid Id,
    DateOnly BirthDate,
    Gender Gender,
    ActivityLevel ActivityLevel,
    float Height,
    List<Allergen> Allergies,
    List<DietaryPreference> DietaryPreferences,
    SubscriptionTier SubscriptionTier,
    DateTime CreatedAt
)
{
    /// <summary>Construit la réponse à partir de l'entité <see cref="User"/>.</summary>
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


