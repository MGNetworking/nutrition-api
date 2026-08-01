using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Repositories;

/// <summary>Accès aux données de l'entité <see cref="Meal"/> via EF Core.</summary>
public sealed class MealRepository : IMealRepository
{
    private readonly AppDbContext _context;

    public MealRepository(AppDbContext context) => _context = context;

    /// <summary>Retourne un repas par son identifiant, avec ses items et l'aliment de chaque item.</summary>
    /// <param name="id">Identifiant du repas.</param>
    /// <returns>Le repas correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    public async Task<Meal?> GetByIdAsync(Guid id)
        => await _context.Meals
            .Include(m => m.MealItems)
                .ThenInclude(mi => mi.FoodItem)
            .FirstOrDefaultAsync(m => m.Id == id);

    /// <summary>Retourne les repas d'un utilisateur, avec leurs items, du plus récent au plus ancien.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="date">Restreint aux repas consommés ce jour-là (UTC).</param>
    /// <param name="saved">Restreint aux repas sauvegardés ou non sauvegardés.</param>
    /// <returns>Liste des repas correspondant aux critères, vide si aucun.</returns>
    public async Task<List<Meal>> GetByUserIdAsync(Guid userId, DateOnly? date = null, bool? saved = null)
    {
        var query = _context.Meals
            .Include(m => m.MealItems)
                .ThenInclude(mi => mi.FoodItem)
            .Where(m => m.UserId == userId);

        if (date is not null)
        {
            // Comparaison sur un intervalle : ConsumedAt est un timestamptz, la date est en UTC
            var start = DateTime.SpecifyKind(date.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var end = start.AddDays(1);
            query = query.Where(m => m.ConsumedAt >= start && m.ConsumedAt < end);
        }

        if (saved is not null)
            query = query.Where(m => m.IsSaved == saved.Value);

        return await query
            .OrderByDescending(m => m.ConsumedAt)
            .ToListAsync();
    }

    /// <summary>Ajoute un repas au contexte — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="meal">Repas à ajouter.</param>
    public async Task AddAsync(Meal meal)
        => await _context.Meals.AddAsync(meal);

    /// <summary>Marque un repas comme modifié — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="meal">Repas avec les données modifiées.</param>
    public Task UpdateAsync(Meal meal)
    {
        _context.Meals.Update(meal);
        return Task.CompletedTask;
    }

    /// <summary>Marque un repas comme supprimé — ses items sont supprimés en cascade. Sans effet si l'identifiant n'existe pas.</summary>
    /// <param name="id">Identifiant du repas à supprimer.</param>
    public async Task DeleteAsync(Guid id)
    {
        var meal = await _context.Meals.FindAsync(id);
        if (meal is not null)
            _context.Meals.Remove(meal);
    }

    /// <summary>Retourne le nombre de repas sauvegardés d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Nombre de repas sauvegardés.</returns>
    public async Task<int> CountSavedByUserIdAsync(Guid userId)
        => await _context.Meals.CountAsync(m => m.UserId == userId && m.IsSaved);

    /// <summary>Retourne le nombre de repas créés depuis une date, tous utilisateurs confondus.</summary>
    /// <param name="since">Date de saisie minimale (UTC).</param>
    /// <returns>Nombre de repas dont <c>CreatedAt</c> est postérieur ou égal à la date.</returns>
    public async Task<int> CountCreatedSinceAsync(DateTime since)
        => await _context.Meals.CountAsync(m => m.CreatedAt >= since);
}
