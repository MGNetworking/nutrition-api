namespace NutritionApi.Infrastructure.Observability;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Data.Common;

/// <summary>
/// Signale les requêtes SQL dont l'exécution dépasse un seuil (NTR-141).
/// </summary>
/// <remarks>
/// NTR-140 inscrit chaque commande SQL dans la trace, avec sa durée — l'information existe donc.
/// Ce qui manquait est le seuil et la journalisation dédiée : une requête anormalement lente doit
/// laisser une ligne, sans qu'un collecteur soit déployé pour la retrouver.
/// <para>
/// Le seuil est un réglage, pas une constante en dur : la lenteur dépend de la charge et du matériel,
/// et un seuil figé serait faux ailleurs qu'en production. La valeur par défaut
/// (<see cref="SeuilParDefautMs"/>) ne sert qu'en l'absence de configuration.
/// </para>
/// <para>
/// L'intercepteur journalise sans jamais rien intercepter : il ne modifie ni la commande ni son
/// résultat, et ne masque aucun échec.
/// </para>
/// </remarks>
public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    /// <summary>Seuil appliqué lorsque la configuration n'en impose pas d'autre, en millisecondes.</summary>
    public const int SeuilParDefautMs = 500;

    /// <summary>
    /// Clé de configuration du seuil. Le préfixe est celui de
    /// <see cref="DatabaseSupervisionService.SectionConfiguration"/>, pour que le réglage de la
    /// surveillance de la base soit groupé.
    /// </summary>
    public const string CleDuSeuil = $"{DatabaseSupervisionService.SectionConfiguration}:SlowQueryThresholdMs";

    private readonly ILogger<SlowQueryInterceptor> _logger;
    private readonly TimeSpan _seuil;

    /// <summary>Construit l'intercepteur.</summary>
    /// <param name="logger">
    /// Journaliseur dédié. Sa catégorie est celle de cette classe, ce qui permet de régler son
    /// niveau indépendamment du reste de la journalisation.
    /// </param>
    /// <param name="seuilMillisecondes">Seuil de lenteur, en millisecondes. Strictement positif.</param>
    /// <exception cref="ArgumentOutOfRangeException">Le seuil est nul ou négatif.</exception>
    public SlowQueryInterceptor(ILogger<SlowQueryInterceptor> logger, int seuilMillisecondes = SeuilParDefautMs)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(seuilMillisecondes);

        _logger = logger;
        _seuil = TimeSpan.FromMilliseconds(seuilMillisecondes);
    }

    /// <inheritdoc />
    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Signaler(command, eventData.Duration);
        return base.ReaderExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        Signaler(command, eventData.Duration);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Signaler(command, eventData.Duration);
        return base.NonQueryExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        Signaler(command, eventData.Duration);
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Signaler(command, eventData.Duration);
        return base.ScalarExecuted(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<object?> ScalarExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        object? result,
        CancellationToken cancellationToken = default)
    {
        Signaler(command, eventData.Duration);
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Journalise la requête si sa durée atteint le seuil.
    /// </summary>
    /// <param name="command">Commande exécutée — son <c>CommandText</c> est repris tel quel.</param>
    /// <param name="duree">Durée d'exécution mesurée par EF Core.</param>
    /// <remarks>
    /// Méthode publique, comme <c>DatabaseExceptionInterceptor.Translate</c> : c'est le point de
    /// décision, extrait du branchement EF Core pour être éprouvé sans base réelle.
    /// </remarks>
    public void Signaler(DbCommand command, TimeSpan duree)
    {
        // Seuil inclusif : une requête exactement au seuil est déjà lente pour ce seuil.
        if (duree < _seuil)
            return;

        _logger.LogWarning(
            "Requête SQL lente : {DureeMs} ms (seuil {SeuilMs} ms). SQL : {Sql}",
            (long)duree.TotalMilliseconds,
            (long)_seuil.TotalMilliseconds,
            command.CommandText);
    }
}
