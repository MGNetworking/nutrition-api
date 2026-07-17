namespace NutritionApi.Application.Interfaces.Repositories;

using NutritionApi.Domain.Entity;

/// <summary>Contrat d'accès aux données pour l'entité <see cref="Meal"/>.</summary>
public interface IMealRepository
{
    /// <summary>Retourne un repas par son identifiant.</summary>
    /// <param name="id">Identifiant du repas.</param>
    /// <returns>Le repas correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    Task<Meal?> GetByIdAsync(Guid id);

    /// <summary>Retourne les repas d'un utilisateur avec filtrage optionnel.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="date">Filtre sur une date de consommation.</param>
    /// <param name="saved">Filtre sur les repas sauvegardés uniquement.</param>
    /// <returns>Liste des repas correspondant aux critères, vide si aucun.</returns>
    Task<List<Meal>> GetByUserIdAsync(Guid userId, DateOnly? date = null, bool? saved = null);

    /// <summary>Persiste un nouveau repas.</summary>
    /// <param name="meal">Repas à ajouter.</param>
    Task AddAsync(Meal meal);

    /// <summary>Met à jour un repas existant.</summary>
    /// <param name="meal">Repas avec les données modifiées.</param>
    Task UpdateAsync(Meal meal);

    /// <summary>Supprime un repas par son identifiant.</summary>
    /// <param name="id">Identifiant du repas à supprimer.</param>
    Task DeleteAsync(Guid id);

    /// <summary>Retourne le nombre de repas sauvegardés d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Nombre de repas sauvegardés.</returns>
    Task<int> CountSavedByUserIdAsync(Guid userId);

    /// <summary>Retourne le nombre de repas saisis depuis une date, tous utilisateurs confondus.</summary>
    /// <param name="since">Date de saisie minimale (UTC).</param>
    /// <returns>Nombre de repas dont <c>CreatedAt</c> est postérieur ou égal à <paramref name="since"/>.</returns>
    Task<int> CountCreatedSinceAsync(DateTime since);
}
