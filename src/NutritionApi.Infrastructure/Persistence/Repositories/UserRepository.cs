using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Infrastructure.Persistence.Repositories;

/// <summary>Accès aux données de l'entité <see cref="User"/> via EF Core.</summary>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    /// <summary>Retourne le nombre d'utilisateurs d'un palier d'abonnement.</summary>
    /// <param name="tier">Palier d'abonnement.</param>
    /// <returns>Nombre d'utilisateurs de ce palier.</returns>
    public async Task<int> CountByTierAsync(SubscriptionTier tier)
        => await _context.Users.CountAsync(u => u.SubscriptionTier == tier);

    /// <summary>Retourne le nombre d'utilisateurs créés depuis une date.</summary>
    /// <param name="since">Date de création minimale (UTC).</param>
    /// <returns>Nombre d'utilisateurs dont <c>CreatedAt</c> est postérieur ou égal à la date.</returns>
    public async Task<int> CountCreatedSinceAsync(DateTime since)
        => await _context.Users.CountAsync(u => u.CreatedAt >= since);

    /// <summary>Retourne le nombre de comptes dont la suppression est demandée depuis moins de 30 jours.</summary>
    /// <returns>Nombre d'utilisateurs en grace period.</returns>
    public async Task<int> CountInGracePeriodAsync()
    {
        var limit = DateTime.UtcNow.AddDays(-30);
        return await _context.Users.CountAsync(u => u.DeletedAt != null && u.DeletedAt > limit);
    }

    /// <summary>Retourne les comptes dont la demande de suppression précède la date limite.</summary>
    /// <param name="limit">Date limite (UTC).</param>
    /// <returns>Les utilisateurs dont <c>DeletedAt</c> est renseigné et antérieur ou égal à la limite.</returns>
    public async Task<IReadOnlyList<User>> GetExpiredForPurgeAsync(DateTime limit)
        => await _context.Users
            .Where(u => u.DeletedAt != null && u.DeletedAt <= limit)
            .ToListAsync();

    /// <summary>Retourne un utilisateur par son identifiant.</summary>
    /// <param name="id">Identifiant de l'utilisateur.</param>
    /// <returns>L'utilisateur correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    public async Task<User?> GetByIdAsync(Guid id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    /// <summary>Retourne un utilisateur par son identifiant Keycloak.</summary>
    /// <param name="keycloakId">Identifiant Keycloak de l'utilisateur.</param>
    /// <returns>L'utilisateur correspondant, ou <c>null</c> s'il n'existe pas.</returns>
    public async Task<User?> GetByKeycloakIdAsync(string keycloakId)
        => await _context.Users.FirstOrDefaultAsync(u => u.KeycloakId == keycloakId);

    /// <summary>Ajoute un utilisateur au contexte — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="user">Utilisateur à ajouter.</param>
    public async Task AddAsync(User user)
        => await _context.Users.AddAsync(user);

    /// <summary>Marque un utilisateur comme modifié — la persistance est déclenchée par l'unité de travail.</summary>
    /// <param name="user">Utilisateur avec les données modifiées.</param>
    public Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Marque un utilisateur comme supprimé — la persistance est déclenchée par l'unité de travail.
    /// Repas, éléments de repas, pesées, aliments enregistrés, plans et régimes partent avec lui :
    /// les clés étrangères qui pointent vers <c>users</c> sont toutes en <c>ON DELETE CASCADE</c>.
    /// </summary>
    /// <param name="user">Utilisateur à supprimer.</param>
    public Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }
}
