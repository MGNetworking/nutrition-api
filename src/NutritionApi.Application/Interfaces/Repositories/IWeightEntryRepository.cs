using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.Interfaces.Repositories;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="WeightEntry"/>.</summary>
public interface IWeightEntryRepository
{
    /// <summary>Retourne une pesée par son identifiant.</summary>
    /// <param name="id">Identifiant de la pesée.</param>
    /// <returns>La pesée correspondante, ou <c>null</c> si elle n'existe pas.</returns>
    Task<WeightEntry?> GetByIdAsync(Guid id);

    /// <summary>Retourne la pesée d'un utilisateur à une date donnée.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="date">Date de la pesée.</param>
    /// <returns>La pesée correspondante, ou <c>null</c> si aucune pesée n'existe à cette date.</returns>
    Task<WeightEntry?> GetByUserIdAndDateAsync(Guid userId, DateOnly date);

    /// <summary>Retourne toutes les pesées d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des pesées de l'utilisateur, vide si aucune.</returns>
    Task<List<WeightEntry>> GetByUserIdAsync(Guid userId);

    /// <summary>Persiste une nouvelle pesée.</summary>
    /// <param name="entry">Pesée à ajouter.</param>
    Task AddAsync(WeightEntry entry);

    /// <summary>Met à jour une pesée existante.</summary>
    /// <param name="entry">Pesée avec les données modifiées.</param>
    Task UpdateAsync(WeightEntry entry);
}
