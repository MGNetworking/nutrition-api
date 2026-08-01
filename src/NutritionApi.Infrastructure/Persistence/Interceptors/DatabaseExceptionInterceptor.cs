namespace NutritionApi.Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using NutritionApi.Application.Exceptions;
using System.Data.Common;
using System.Net.Sockets;

/// <summary>
/// Traduit les échecs PostgreSQL en exceptions applicatives, en un seul point pour toutes les
/// commandes EF Core.
/// </summary>
/// <remarks>
/// Sans lui, la couche API devrait connaître Npgsql pour distinguer un conflit d'une panne — ce qui
/// lui ferait référencer un package d'Infrastructure. L'alternative écartée était un <c>try/catch</c>
/// dans chaque méthode de repository, soit une cinquantaine de blocs identiques.
/// </remarks>
public sealed class DatabaseExceptionInterceptor : DbCommandInterceptor
{
    /// <summary>Code PostgreSQL d'une violation de contrainte d'unicité.</summary>
    private const string UniqueViolation = "23505";

    /// <inheritdoc />
    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        var traduite = Translate(eventData.Exception);
        if (traduite is not null)
            throw traduite;
    }

    /// <inheritdoc />
    public override Task CommandFailedAsync(
        DbCommand command,
        CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var traduite = Translate(eventData.Exception);

        return traduite is null ? Task.CompletedTask : Task.FromException(traduite);
    }

    /// <summary>
    /// Détermine l'exception applicative correspondant à un échec de base, ou <c>null</c> si
    /// l'échec n'est pas identifiable — auquel cas l'exception d'origine remonte, donc un 500.
    /// </summary>
    /// <param name="exception">Exception levée par la commande.</param>
    /// <returns>L'exception applicative à lever, ou <c>null</c> pour laisser passer l'originale.</returns>
    public static Exception? Translate(Exception exception)
    {
        // Sur SaveChanges, EF Core enveloppe l'erreur : la discrimination doit porter sur
        // l'exception interne, pas sur le type externe.
        var cause = exception is DbException ? exception : exception.InnerException ?? exception;

        return cause switch
        {
            // Erreur renvoyée par le serveur : la connexion fonctionne, c'est la donnée qui pose
            // problème. Seule la violation d'unicité est traduisible en conflit — les autres codes
            // signalent un défaut applicatif, qui doit rester visible en 500.
            PostgresException postgres =>
                postgres.SqlState == UniqueViolation
                    ? new ConflictException("Cette donnée existe déjà.")
                    : null,

            // Pas de réponse du serveur : la base est injoignable.
            NpgsqlException or SocketException or TimeoutException =>
                new ServiceUnavailableException("PostgreSQL", cause),

            _ => null
        };
    }
}
