using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Repositories;

/// <summary>Accès aux données de l'entité <see cref="FoodItem"/> via EF Core.</summary>
public sealed class FoodItemRepository : IFoodItemRepository
{
    private readonly AppDbContext _context;

    public FoodItemRepository(AppDbContext context) => _context = context;

    /// <summary>Retourne un aliment par son identifiant.</summary>
    /// <param name="id">Identifiant de l'aliment.</param>
    /// <returns>L'aliment correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    public async Task<FoodItem?> GetByIdAsync(Guid id)
        => await _context.FoodItems.FirstOrDefaultAsync(f => f.Id == id);

    /// <summary>Retourne les aliments correspondant à une liste d'identifiants.</summary>
    /// <param name="ids">Liste des identifiants d'aliments.</param>
    /// <returns>Liste des aliments trouvés — peut contenir moins d'éléments que la liste fournie.</returns>
    public async Task<List<FoodItem>> GetByIdsAsync(List<Guid> ids)
        => await _context.FoodItems
            .Where(f => ids.Contains(f.Id))
            .ToListAsync();

    /// <summary>Retourne un aliment par son identifiant Open Food Facts.</summary>
    /// <param name="offId">Identifiant Open Food Facts de l'aliment.</param>
    /// <returns>L'aliment correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    public async Task<FoodItem?> GetByOffIdAsync(string offId)
        => await _context.FoodItems.FirstOrDefaultAsync(f => f.OffId == offId);

    /// <summary>Recherche les aliments dont le nom contient le mot-clé, sans distinction de casse.</summary>
    /// <param name="keyword">Mot-clé de recherche.</param>
    /// <param name="limit">Nombre maximum de résultats.</param>
    /// <returns>Liste des aliments correspondants, vide si aucun.</returns>
    public async Task<List<FoodItem>> SearchByKeywordAsync(string keyword, int limit = 20)
        => await _context.FoodItems
            .Where(f => EF.Functions.ILike(f.Name, $"%{keyword}%"))
            .OrderBy(f => f.Name)
            .Take(limit)
            .ToListAsync();

    /// <summary>Ajoute un aliment au contexte — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="foodItem">Aliment à ajouter.</param>
    public async Task AddAsync(FoodItem foodItem)
        => await _context.FoodItems.AddAsync(foodItem);

    /// <summary>Marque un aliment comme modifié — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="foodItem">Aliment avec les données modifiées.</param>
    public Task UpdateAsync(FoodItem foodItem)
    {
        _context.FoodItems.Update(foodItem);
        return Task.CompletedTask;
    }

    /// <summary>Retourne le nombre total d'aliments du catalogue.</summary>
    /// <returns>Nombre d'aliments.</returns>
    public async Task<int> CountAsync()
        => await _context.FoodItems.CountAsync();
}
