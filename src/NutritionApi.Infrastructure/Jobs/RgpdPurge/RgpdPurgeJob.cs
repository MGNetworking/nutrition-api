namespace NutritionApi.Infrastructure.Jobs.RgpdPurge;

using Microsoft.Extensions.Logging;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;

/// <summary>
/// Implémentation de <see cref="IRgpdPurgeJob"/> : supprime définitivement les comptes dont la
/// demande de suppression dépasse la grace period, dans Keycloak puis dans PostgreSQL.
/// </summary>
public sealed class RgpdPurgeJob : IRgpdPurgeJob
{
    /// <summary>Durée pendant laquelle un compte marqué pour suppression reste récupérable.</summary>
    private const int GracePeriodDays = 30;

    private readonly IUserRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKeycloakAdminService _keycloak;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<RgpdPurgeJob> _logger;

    /// <summary>Construit le job de purge.</summary>
    /// <param name="users">Accès aux comptes utilisateurs.</param>
    /// <param name="unitOfWork">Unité de travail — un commit par compte purgé.</param>
    /// <param name="keycloak">Administration des comptes Keycloak.</param>
    /// <param name="timeProvider">Horloge utilisée pour calculer la fin de la grace period.</param>
    /// <param name="logger">Journalisation du bilan de purge.</param>
    public RgpdPurgeJob(
        IUserRepository users,
        IUnitOfWork unitOfWork,
        IKeycloakAdminService keycloak,
        TimeProvider timeProvider,
        ILogger<RgpdPurgeJob> logger)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _keycloak = keycloak;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <summary>
    /// Purge chaque compte expiré : suppression du compte Keycloak, puis des données PostgreSQL.
    /// Un échec n'interrompt pas les comptes suivants, mais fait échouer le job en fin de parcours
    /// afin que Hangfire le replanifie.
    /// </summary>
    /// <exception cref="InvalidOperationException">Au moins un compte n'a pas pu être purgé.</exception>
    public async Task RunAsync()
    {
        var limit = _timeProvider.GetUtcNow().UtcDateTime.AddDays(-GracePeriodDays);
        var expired = await _users.GetExpiredForPurgeAsync(limit);

        if (expired.Count == 0)
        {
            _logger.LogInformation("Purge RGPD : aucun compte à supprimer.");
            return;
        }

        var purged = 0;
        var failed = 0;

        foreach (var user in expired)
        {
            if (await TryPurgeAsync(user))
                purged++;
            else
                failed++;
        }

        _logger.LogInformation(
            "Purge RGPD terminée : {Purged} comptes supprimés, {Failed} en échec.",
            purged, failed);

        // Hangfire ne replanifie que sur exception : sans cela, les comptes en échec resteraient
        // en attente jusqu'à l'exécution du lendemain.
        if (failed > 0)
            throw new InvalidOperationException($"Purge RGPD : {failed} compte(s) n'ont pas pu être supprimés.");
    }

    /// <summary>
    /// Supprime un compte, Keycloak d'abord. Cet ordre garantit l'idempotence : si Keycloak échoue,
    /// rien n'est supprimé et le compte reste sélectionnable au prochain passage ; si la base échoue
    /// ensuite, le compte Keycloak est déjà parti et le nouvel appel se soldera par un 404 ignoré.
    /// L'ordre inverse laisserait un compte Keycloak orphelin, plus jamais sélectionné.
    /// </summary>
    /// <param name="user">Compte à purger.</param>
    /// <returns><c>true</c> si le compte a été entièrement supprimé.</returns>
    private async Task<bool> TryPurgeAsync(User user)
    {
        try
        {
            await _keycloak.DeleteUserAsync(user.KeycloakId);

            await _users.DeleteAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Purge RGPD : échec de la suppression du compte {UserId}.",
                user.Id);

            return false;
        }
    }
}
