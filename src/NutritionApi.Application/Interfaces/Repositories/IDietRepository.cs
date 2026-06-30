namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="Diet"/>.</summary>
public interface IDietRepository
{
    /// <summary>Retourne un régime par son identifiant.</summary>
    /// <param name="id">Identifiant du régime.</param>
    /// <returns>Le régime correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    Task<Diet?> GetByIdAsync(Guid id);

    /// <summary>Retourne le régime actif de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Le régime actif, ou <c>null</c> si aucun régime n'est actif.</returns>
    Task<Diet?> GetActiveByUserIdAsync(Guid userId);

    /// <summary>Retourne tous les régimes d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des régimes de l'utilisateur, vide si aucun.</returns>
    Task<List<Diet>> GetByUserIdAsync(Guid userId);

    /// <summary>Persiste un nouveau régime.</summary>
    /// <param name="diet">Régime à ajouter.</param>
    Task AddAsync(Diet diet);

    /// <summary>Met à jour un régime existant.</summary>
    /// <param name="diet">Régime avec les données modifiées.</param>
    Task UpdateAsync(Diet diet);
}
