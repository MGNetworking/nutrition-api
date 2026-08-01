using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Repositories;

/// <summary>Accès aux données de l'entité <see cref="DietPlan"/> via EF Core.</summary>
public sealed class DietPlanRepository : IDietPlanRepository
{
    private readonly AppDbContext _context;

    public DietPlanRepository(AppDbContext context) => _context = context;

    /// <summary>Retourne un plan nutritionnel par son identifiant.</summary>
    /// <param name="planId">Identifiant du plan.</param>
    /// <returns>Le plan correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    public async Task<DietPlan?> GetByIdAsync(Guid planId)
        => await _context.DietPlans.FirstOrDefaultAsync(p => p.Id == planId);

    /// <summary>Retourne les plans personnels d'un utilisateur — les templates, sans propriétaire, sont exclus.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des plans de l'utilisateur, vide si aucun.</returns>
    public async Task<List<DietPlan>> GetByUserIdAsync(Guid userId)
        => await _context.DietPlans
            .Where(p => p.UserId == userId)
            .OrderBy(p => p.Name)
            .ToListAsync();

    /// <summary>Retourne les plans marqués comme templates partagés.</summary>
    /// <returns>Liste des templates disponibles, vide si aucun.</returns>
    public async Task<List<DietPlan>> GetTemplatesAsync()
        => await _context.DietPlans
            .Where(p => p.IsTemplate)
            .OrderBy(p => p.Name)
            .ToListAsync();

    /// <summary>Ajoute un plan au contexte — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="dietPlan">Plan à ajouter.</param>
    public async Task AddAsync(DietPlan dietPlan)
        => await _context.DietPlans.AddAsync(dietPlan);

    /// <summary>Marque un plan comme modifié — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="dietPlan">Plan avec les données modifiées.</param>
    public Task UpdateAsync(DietPlan dietPlan)
    {
        _context.DietPlans.Update(dietPlan);
        return Task.CompletedTask;
    }

    /// <summary>Marque un plan comme supprimé — sans effet si l'identifiant n'existe pas.</summary>
    /// <param name="id">Identifiant du plan à supprimer.</param>
    public async Task DeleteAsync(Guid id)
    {
        var plan = await _context.DietPlans.FindAsync(id);
        if (plan is not null)
            _context.DietPlans.Remove(plan);
    }

    /// <summary>Retourne le nombre de plans personnels d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Nombre de plans appartenant à l'utilisateur.</returns>
    public async Task<int> CountByUserIdAsync(Guid userId)
        => await _context.DietPlans.CountAsync(p => p.UserId == userId);
}
