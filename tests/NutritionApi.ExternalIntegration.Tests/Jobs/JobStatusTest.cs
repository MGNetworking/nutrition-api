namespace NutritionApi.ExternalIntegration.Tests.Jobs;

using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Infrastructure.Scheduling;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// La supervision des jobs récurrents, lue dans <c>hangfire.hash</c>.
/// </summary>
/// <remarks>
/// <c>JobMonitoringService</c> interroge le stockage Hangfire en SQL brut : ses tables, ses noms de
/// champs et le format de ses dates sont des contrats externes. Un test unitaire ne pourrait que
/// simuler ces réponses ; seul un vrai stockage confirme que la requête est juste.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class JobStatusTest(IntegrationFactory factory)
{
    /// <summary>
    /// sans aucun job enregistré, la supervision renvoie une liste vide.
    /// </summary>
    /// <remarks>
    /// L'API enregistre ses jobs au démarrage : les définitions sont donc supprimées le temps du test,
    /// puis réinscrites par le service d'enregistrement — dont l'appel est idempotent. Sans cette
    /// remise en état, les cas suivants ne trouveraient plus rien.
    /// </remarks>
    [Fact]
    public async Task GetJobsStatusAsync_ShouldReturnEmptyList_WhenNoRecurringJobIsRegistered()
    {
        await using (var context = factory.NewContext())
        {
            await context.Database.ExecuteSqlRawAsync(
                "delete from hangfire.hash where key like 'recurring-job:%';");
        }

        try
        {
            var (scope, monitoring) = factory.Resolve<IJobMonitoringService>();

            using (scope)
                Assert.Empty(await monitoring.GetJobsStatusAsync());
        }
        finally
        {
            await ReregisterJobsAsync();
        }
    }

    /// <summary>
    /// un job enregistré mais jamais exécuté est <c>Scheduled</c>, sans dernière
    /// exécution.
    /// </summary>
    /// <remarks>
    /// La purge RGPD sert de témoin : aucun test ne la déclenche, son état reste donc celui d'un job
    /// fraîchement inscrit. <c>LastJobState</c> est absent du hash, et c'est le service qui en déduit
    /// <c>Scheduled</c> — la valeur ne vient pas de Hangfire.
    /// </remarks>
    [Fact]
    public async Task GetJobsStatusAsync_ShouldReturnScheduled_WhenJobHasNeverRun()
    {
        await ReregisterJobsAsync();

        var (scope, monitoring) = factory.Resolve<IJobMonitoringService>();

        using (scope)
        {
            var jobs = await monitoring.GetJobsStatusAsync();
            var purge = jobs.Single(job => job.JobName == IJobMonitoringService.RgpdPurgeJobName);

            Assert.Null(purge.LastRun);
            Assert.NotNull(purge.NextRun);
            Assert.Equal("Scheduled", purge.Status);
        }
    }

    /// <summary>
    /// après une exécution réussie, la dernière exécution est renseignée et le run
    /// aboutit à l'état <c>Succeeded</c>.
    /// </summary>
    /// <remarks>
    /// Ce cas a mis au jour un défaut de <c>JobMonitoringService</c>, corrigé depuis. Un déclenchement
    /// manuel écrit <c>LastExecution</c> et <c>LastJobId</c> dans <c>hangfire.hash</c>, mais <b>pas</b>
    /// <c>LastJobState</c> : le service, qui n'en lisait que ce dernier champ, exposait <c>Scheduled</c>
    /// alors que <c>lastRun</c> était renseigné. Il joint désormais <c>hangfire.job</c> sur
    /// <c>LastJobId</c> pour obtenir l'état réel du run.
    /// <para>
    /// C'est le seul cas de la suite qui dépende d'un traitement de fond : le serveur Hangfire doit
    /// prendre le job déclenché et le mener à son terme. Il s'exécutait autrefois sur une
    /// application dédiée, parce que le serveur partagé cessait de traiter en cours de suite — cause
    /// élucidée par NTR-156, corrigée dans <c>IntegrationFactory</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task GetJobsStatusAsync_ShouldReturnSucceeded_WhenJobRanSuccessfully()
    {
        // Source de dump vide : l'import se termine sans rien écrire, ce qui suffit à produire un
        // run réussi.
        factory.DumpLines.Clear();

        factory.Services.GetRequiredService<IRecurringJobManager>()
               .Trigger(IJobMonitoringService.ImportOffJobName);

        var (scope, monitoring) = factory.Resolve<IJobMonitoringService>();

        using (scope)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(60);
            HangfireJobResponse? importOff = null;

            while (DateTime.UtcNow < deadline)
            {
                var candidat = (await monitoring.GetJobsStatusAsync())
                    .FirstOrDefault(job => job.JobName == IJobMonitoringService.ImportOffJobName);

                // Tant que le serveur Hangfire n'a pas fini, le run est Enqueued ou Processing.
                if (candidat is { Status: not "Scheduled" and not "Enqueued" and not "Processing" })
                {
                    importOff = candidat;
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(2));
            }

            Assert.True(
                importOff is not null,
                $"Aucun run terminé après 60 s. Contenu du hash :\n{await DumpHashAsync(factory)}");

            Assert.Equal("Succeeded", importOff!.Status);
            Assert.NotNull(importOff.LastRun);
        }
    }

    /// <summary>
    /// le job d'import est exposé sous le nom exact <c>import-off</c>.
    /// </summary>
    /// <remarks>
    /// <c>AdminService</c> remonte <c>LastImportAt</c> à la racine de la réponse en cherchant ce nom
    /// précis. Un renommage côté enregistrement viderait ce champ sans qu'aucun autre test ne le voie.
    /// </remarks>
    [Fact]
    public async Task GetJobsStatusAsync_ShouldExposeImportOffJobName_WhenJobIsRegistered()
    {
        await ReregisterJobsAsync();

        var (scope, monitoring) = factory.Resolve<IJobMonitoringService>();

        using (scope)
        {
            var jobs = await monitoring.GetJobsStatusAsync();

            Assert.Contains("import-off", jobs.Select(job => job.JobName));
            Assert.Equal("import-off", IJobMonitoringService.ImportOffJobName);
        }
    }

    /// <summary>Rejoue l'enregistrement des jobs récurrents — idempotent par conception.</summary>
    private async Task ReregisterJobsAsync()
    {
        var registration = factory.Services.GetServices<IHostedService>()
                                  .OfType<RecurringJobRegistrationService>()
                                  .Single();

        await registration.StartAsync(CancellationToken.None);
    }

    /// <summary>
    /// Restitue le contenu brut de <c>hangfire.hash</c> pour les jobs récurrents.
    /// </summary>
    /// <returns>Une ligne par champ, au format <c>clé | champ = valeur</c>.</returns>
    /// <remarks>
    /// Sert de message d'échec : quand un statut n'évolue pas comme attendu, la cause est presque
    /// toujours un champ que Hangfire n'écrit pas, ou pas au moment supposé. Sans ce contenu, l'échec
    /// n'indique rien d'exploitable.
    /// </remarks>
    private static async Task<string> DumpHashAsync(IntegrationFactory fabrique)
    {
        await using var connexion = new Npgsql.NpgsqlConnection(fabrique.Database.ConnectionString);

        await connexion.OpenAsync();

        await using var commande = new Npgsql.NpgsqlCommand(
            "select key, field, value from hangfire.hash where key like 'recurring-job:%' order by key, field;",
            connexion);

        var lignes = new List<string>();

        await using var lecteur = await commande.ExecuteReaderAsync();

        while (await lecteur.ReadAsync())
            lignes.Add($"  {lecteur.GetString(0)} | {lecteur.GetString(1)} = {lecteur.GetString(2)}");

        return lignes.Count == 0 ? "  (vide)" : string.Join('\n', lignes);
    }
}
