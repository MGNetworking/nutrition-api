using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.Interfaces.Repositories;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="User"/>.</summary>
public interface IUserRepository
{
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
}
