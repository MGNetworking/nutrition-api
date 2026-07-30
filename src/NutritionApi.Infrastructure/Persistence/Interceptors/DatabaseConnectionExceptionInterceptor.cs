namespace NutritionApi.Infrastructure.Persistence.Interceptors;

using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

/// <summary>
/// Traduit les échecs d'ouverture de connexion PostgreSQL en exceptions applicatives.
/// </summary>
/// <remarks>
/// Complément indispensable de <see cref="DatabaseExceptionInterceptor"/>, qui n'intercepte que les
/// échecs de <b>commande</b>. Quand le serveur est arrêté, la commande n'est jamais atteinte : l'échec
/// survient à l'ouverture de la connexion, et rien n'était traduit — la couche API répondait 500
/// là où elle doit répondre 503.
/// <para>
/// Le défaut a été mis au jour par IT-EXT-14 (NTR-28), qui arrête le conteneur PostgreSQL avant
/// d'appeler un endpoint de lecture. Un intercepteur de commande ne pouvait pas couvrir ce cas.
/// </para>
/// <para>
/// La traduction est déléguée à <see cref="DatabaseExceptionInterceptor.Translate"/> : une seule
/// table de correspondance entre échecs PostgreSQL et exceptions applicatives, quelle que soit
/// l'étape où l'échec survient.
/// </para>
/// </remarks>
public sealed class DatabaseConnectionExceptionInterceptor : DbConnectionInterceptor
{
    /// <inheritdoc />
    public override void ConnectionFailed(DbConnection connection, ConnectionErrorEventData eventData)
    {
        var traduite = DatabaseExceptionInterceptor.Translate(eventData.Exception);

        if (traduite is not null)
            throw traduite;
    }

    /// <inheritdoc />
    public override Task ConnectionFailedAsync(
        DbConnection connection,
        ConnectionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var traduite = DatabaseExceptionInterceptor.Translate(eventData.Exception);

        return traduite is null ? Task.CompletedTask : Task.FromException(traduite);
    }
}
