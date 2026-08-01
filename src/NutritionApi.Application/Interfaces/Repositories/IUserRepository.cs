using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Interfaces.Repositories;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="User"/>.</summary>
public interface IUserRepository
{
    /// <summary>Retourne le nombre d'utilisateurs d'un palier d'abonnement.</summary>
    /// <param name="tier">Palier d'abonnement.</param>
    /// <returns>Nombre d'utilisateurs de ce palier.</returns>
    Task<int> CountByTierAsync(SubscriptionTier tier);

    /// <summary>Retourne le nombre d'utilisateurs créés depuis une date.</summary>
    /// <param name="since">Date de création minimale (UTC).</param>
    /// <returns>Nombre d'utilisateurs dont <c>CreatedAt</c> est postérieur ou égal à <paramref name="since"/>.</returns>
    Task<int> CountCreatedSinceAsync(DateTime since);

    /// <summary>Retourne le nombre de comptes en grace period.</summary>
    /// <returns>Nombre d'utilisateurs dont <c>DeletedAt</c> est renseigné et date de moins de 30 jours.</returns>
    Task<int> CountInGracePeriodAsync();

    /// <summary>Retourne les comptes dont la demande de suppression précède une date donnée.</summary>
    /// <param name="limit">Date limite (UTC) — les comptes marqués avant sont retournés.</param>
    /// <returns>Les utilisateurs concernés, ou une liste vide.</returns>
    Task<IReadOnlyList<User>> GetExpiredForPurgeAsync(DateTime limit);

    /// <summary>Retourne un utilisateur par son identifiant.</summary>
    /// <param name="id">Identifiant de l'utilisateur.</param>
    /// <returns>L'utilisateur correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>Retourne un utilisateur par son identifiant Keycloak.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <returns>L'utilisateur correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    Task<User?> GetByKeycloakIdAsync(string keycloakId);

    /// <summary>Persiste un nouvel utilisateur.</summary>
    /// <param name="user">Utilisateur à ajouter.</param>
    Task AddAsync(User user);

    /// <summary>Met à jour un utilisateur existant.</summary>
    /// <param name="user">Utilisateur avec les données modifiées.</param>
    Task UpdateAsync(User user);

    /// <summary>Supprime un utilisateur et les données qui en dépendent.</summary>
    /// <param name="user">Utilisateur à supprimer.</param>
    Task DeleteAsync(User user);
}
