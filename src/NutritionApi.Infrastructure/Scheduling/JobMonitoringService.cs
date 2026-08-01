namespace NutritionApi.Infrastructure.Scheduling;

using System.Globalization;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.Interfaces.ExternalServices;

/// <summary>
/// Implémentation de <see cref="IJobMonitoringService"/> qui lit l'état des jobs
/// récurrents directement dans le stockage Hangfire (schéma <c>hangfire</c>, table <c>hash</c>).
/// </summary>
/// <remarks>
/// Les jobs récurrents sont stockés sous la clé <c>recurring-job:&lt;id&gt;</c>. La table
/// <c>hash</c> porte la dernière exécution, la prochaine exécution planifiée et l'état du
/// dernier run — contrairement à l'historique brut (tables <c>job</c>/<c>state</c>), qui ne
/// donne pas la prochaine exécution.
/// </remarks>
public sealed class JobMonitoringService : IJobMonitoringService
{
    private readonly string _connectionString;

    /// <summary>Construit le service à partir de la chaîne de connexion <c>DefaultConnection</c>.</summary>
    /// <param name="configuration">Configuration de l'application.</param>
    /// <exception cref="InvalidOperationException">La chaîne de connexion <c>DefaultConnection</c> est absente.</exception>
    public JobMonitoringService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("La chaîne de connexion 'DefaultConnection' est absente.");
    }

    /// <summary>Retourne le statut de chaque job récurrent enregistré dans Hangfire.</summary>
    /// <returns>La liste des jobs planifiés — vide si aucun n'est enregistré.</returns>
    public async Task<List<HangfireJobResponse>> GetJobsStatusAsync()
    {
        const string sql = """
            with recurring as (
                select
                    substring(key from 'recurring-job:(.*)')          as job_name,
                    max(value) filter (where field = 'LastExecution') as last_run,
                    max(value) filter (where field = 'NextExecution') as next_run,
                    max(value) filter (where field = 'LastJobState')  as last_job_state,
                    max(value) filter (where field = 'LastJobId')     as last_job_id
                from hangfire.hash
                where key like 'recurring-job:%'
                group by key
            )
            select
                r.job_name,
                r.last_run,
                r.next_run,
                -- L'état de la ligne de job prime sur LastJobState : Hangfire n'écrit pas ce champ
                -- lors d'un déclenchement manuel, alors que la ligne porte l'état réel. Repli sur
                -- LastJobState quand le job a été purgé de l'historique.
                coalesce(j.statename, r.last_job_state) as status
            from recurring r
            left join hangfire.job j
                   on j.id = nullif(r.last_job_id, '')::bigint
            order by r.job_name;
            """;

        var jobs = new List<HangfireJobResponse>();

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var jobName = reader.GetString(0);
            var lastRun = ParseHangfireDate(reader.IsDBNull(1) ? null : reader.GetString(1));
            var nextRun = ParseHangfireDate(reader.IsDBNull(2) ? null : reader.GetString(2));

            // Ni état de run, ni LastJobState → job enregistré mais jamais exécuté
            var status = reader.IsDBNull(3) ? "Scheduled" : reader.GetString(3);

            jobs.Add(new HangfireJobResponse(jobName, lastRun, nextRun, status));
        }

        return jobs;
    }

    /// <summary>
    /// Parse un horodatage Hangfire ; retourne <c>null</c> si absent ou illisible.
    /// Hangfire.PostgreSql stocke les dates des recurring jobs en millisecondes Unix
    /// (ex : <c>1784862000000</c>) ; un repli ISO 8601 est conservé par prudence.
    /// </summary>
    private static DateTime? ParseHangfireDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixMilliseconds))
            return DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds).UtcDateTime;

        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out var parsed)
            ? parsed
            : null;
    }
}
