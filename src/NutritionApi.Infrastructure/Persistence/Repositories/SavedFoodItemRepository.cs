using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Repositories;

/// <summary>Accès aux données de l'entité <see cref="SavedFoodItem"/> via EF Core.</summary>
public sealed class SavedFoodItemRepository : ISavedFoodItemRepository
{
    private readonly AppDbContext _context;

    public SavedFoodItemRepository(AppDbContext context) => _context = context;

    /// <summary>Retourne un aliment favori par son identifiant.</summary>
    /// <param name="id">Identifiant de l'entrée favori.</param>
    /// <returns>L'entrée correspondante, ou <c>null</c> si elle n'existe pas.</returns>
    public async Task<SavedFoodItem?> GetByIdAsync(Guid id)
        => await _context.SavedFoodItems.FirstOrDefaultAsync(s => s.Id == id);

    /// <summary>Retourne le favori d'un utilisateur pour un aliment donné.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="foodItemId">Identifiant de l'aliment.</param>
    /// <returns>L'entrée correspondante, ou <c>null</c> si elle n'existe pas.</returns>
    public async Task<SavedFoodItem?> GetByUserIdAndFoodItemIdAsync(Guid userId, Guid foodItemId)
        => await _context.SavedFoodItems
            .FirstOrDefaultAsync(s => s.UserId == userId && s.FoodItemId == foodItemId);

    /// <summary>Retourne tous les favoris d'un utilisateur, du plus récent au plus ancien.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des aliments favoris, vide si aucun.</returns>
    public async Task<List<SavedFoodItem>> GetByUserIdAsync(Guid userId)
        => await _context.SavedFoodItems
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SavedAt)
            .ToListAsync();

    /// <summary>Ajoute un favori au contexte — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="savedFoodItem">Aliment favori à ajouter.</param>
    public async Task AddAsync(SavedFoodItem savedFoodItem)
        => await _context.SavedFoodItems.AddAsync(savedFoodItem);

    /// <summary>Marque un favori comme supprimé — sans effet si l'identifiant n'existe pas.</summary>
    /// <param name="id">Identifiant de l'entrée favori à supprimer.</param>
    public async Task DeleteAsync(Guid id)
    {
        var saved = await _context.SavedFoodItems.FindAsync(id);
        if (saved is not null)
            _context.SavedFoodItems.Remove(saved);
    }

    /// <summary>Retourne le nombre de favoris d'un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Nombre d'aliments favoris.</returns>
    public async Task<int> CountByUserIdAsync(Guid userId)
        => await _context.SavedFoodItems.CountAsync(s => s.UserId == userId);
}
