using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Infrastructure.Persistence.Repositories;

/// <summary>Accès aux données de l'entité <see cref="Diet"/> via EF Core.</summary>
public sealed class DietRepository : IDietRepository
{
    private readonly AppDbContext _context;

    public DietRepository(AppDbContext context) => _context = context;

    /// <summary>Retourne un régime par son identifiant.</summary>
    /// <param name="dietId">Identifiant du régime.</param>
    /// <returns>Le régime correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    public async Task<Diet?> GetByIdAsync(Guid dietId)
        => await _context.Diets.FirstOrDefaultAsync(d => d.Id == dietId);

    /// <summary>Retourne le régime de l'utilisateur ayant le statut <c>Active</c>.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Le régime actif, ou <c>null</c> si aucun régime n'est actif.</returns>
    public async Task<Diet?> GetActiveByUserIdAsync(Guid userId)
        => await _context.Diets
            .FirstOrDefaultAsync(d => d.UserId == userId && d.StatusDiet == DietStatus.Active);

    /// <summary>Retourne tous les régimes d'un utilisateur, du plus récent au plus ancien.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des régimes de l'utilisateur, vide si aucun.</returns>
    public async Task<List<Diet>> GetByUserIdAsync(Guid userId)
        => await _context.Diets
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.StartDate)
            .ToListAsync();

    /// <summary>Retourne le nombre de régimes actifs, tous utilisateurs confondus.</summary>
    /// <returns>Nombre de régimes avec le statut <c>Active</c>.</returns>
    public async Task<int> CountActiveAsync()
        => await _context.Diets.CountAsync(d => d.StatusDiet == DietStatus.Active);

    /// <summary>Ajoute un régime au contexte — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="diet">Régime à ajouter.</param>
    public async Task AddAsync(Diet diet)
        => await _context.Diets.AddAsync(diet);

    /// <summary>Marque un régime comme modifié — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="diet">Régime avec les données modifiées.</param>
    public Task UpdateAsync(Diet diet)
    {
        _context.Diets.Update(diet);
        return Task.CompletedTask;
    }
}
