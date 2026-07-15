
namespace NutritionApi.Domain.Enums
{
    /// <summary>Palier d'abonnement de l'utilisateur — conditionne les quotas et l'accès aux fonctionnalités ; source de vérité en base, jamais lue depuis le JWT.</summary>
    public enum SubscriptionTier
    {
        /// <summary>Valeur par défaut non renseignée — rejetée par les invariants du domaine.</summary>
        Unknown = 0,

        /// <summary>Palier gratuit — quotas restreints, pas d'accès aux templates partagés.</summary>
        Free = 1,

        /// <summary>Palier Pro — quotas étendus et accès en lecture aux templates partagés.</summary>
        Pro = 2,

        /// <summary>Palier Business — quotas illimités et accès en lecture aux templates partagés.</summary>
        Business = 3
    }
}
