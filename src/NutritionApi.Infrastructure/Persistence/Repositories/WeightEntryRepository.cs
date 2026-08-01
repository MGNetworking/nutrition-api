using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Repositories;

/// <summary>Accès aux données de l'entité <see cref="WeightEntry"/> via EF Core.</summary>
public sealed class WeightEntryRepository : IWeightEntryRepository
{
    private readonly AppDbContext _context;

    public WeightEntryRepository(AppDbContext context) => _context = context;

    /// <summary>Retourne une pesée par son identifiant.</summary>
    /// <param name="id">Identifiant de la pesée.</param>
    /// <returns>La pesée correspondante, ou <c>null</c> si elle n'existe pas.</returns>
    public async Task<WeightEntry?> GetByIdAsync(Guid id)
        => await _context.WeightEntries.FirstOrDefaultAsync(w => w.Id == id);

    /// <summary>Retourne la pesée d'un utilisateur à une date donnée.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="date">Date de la pesée.</param>
    /// <returns>La pesée correspondante, ou <c>null</c> si aucune pesée n'existe à cette date.</returns>
    public async Task<WeightEntry?> GetByUserIdAndDateAsync(Guid userId, DateOnly date)
        => await _context.WeightEntries
            .FirstOrDefaultAsync(w => w.UserId == userId && w.MeasuredAt == date);

    /// <summary>Retourne toutes les pesées d'un utilisateur, de la plus récente à la plus ancienne.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des pesées de l'utilisateur, vide si aucune.</returns>
    public async Task<List<WeightEntry>> GetByUserIdAsync(Guid userId)
        => await _context.WeightEntries
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.MeasuredAt)
            .ToListAsync();

    /// <summary>Ajoute une pesée au contexte — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="entry">Pesée à ajouter.</param>
    public async Task AddAsync(WeightEntry entry)
        => await _context.WeightEntries.AddAsync(entry);

    /// <summary>Marque une pesée comme modifiée — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="entry">Pesée avec les données modifiées.</param>
    public Task UpdateAsync(WeightEntry entry)
    {
        _context.WeightEntries.Update(entry);
        return Task.CompletedTask;
    }
}
