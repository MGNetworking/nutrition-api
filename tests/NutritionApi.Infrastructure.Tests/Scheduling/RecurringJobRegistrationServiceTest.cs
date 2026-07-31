namespace NutritionApi.Infrastructure.Tests.Scheduling;

using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.Logging;
using Moq;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Infrastructure.Jobs.OffImport;
using NutritionApi.Infrastructure.Jobs.RgpdPurge;
using NutritionApi.Infrastructure.Scheduling;

[Trait("Level", "1")]
public class RecurringJobRegistrationServiceTest
{
    private readonly Mock<IRecurringJobManager> _recurringJobs = new(MockBehavior.Strict);
    private readonly Mock<ILogger<RecurringJobRegistrationService>> _logger = new();

    private readonly List<(string Id, Job Job, string Cron, RecurringJobOptions Options)> _registered = [];

    private RecurringJobRegistrationService CreateService()
        => new(_recurringJobs.Object, _logger.Object);

    /// <summary>Accepte tout enregistrement et le retient — les extensions génériques délèguent à cette surcharge.</summary>
    private void GivenManagerAccepts()
        => _recurringJobs
            .Setup(m => m.AddOrUpdate(
                It.IsAny<string>(),
                It.IsAny<Job>(),
                It.IsAny<string>(),
                It.IsAny<RecurringJobOptions>()))
            .Callback<string, Job, string, RecurringJobOptions>(
                (id, job, cron, options) => _registered.Add((id, job, cron, options)));

    private void GivenStorageUnavailable()
        => _recurringJobs
            .Setup(m => m.AddOrUpdate(
                It.IsAny<string>(),
                It.IsAny<Job>(),
                It.IsAny<string>(),
                It.IsAny<RecurringJobOptions>()))
            .Throws(new InvalidOperationException("storage injoignable"));

    private (string Id, Job Job, string Cron, RecurringJobOptions Options) Registered(string id)
        => _registered.Single(r => r.Id == id);

    // ── Chemin nominal ────────────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_RegistersBothRecurringJobs()
    {
        GivenManagerAccepts();

        await CreateService().StartAsync(CancellationToken.None);

        Assert.Equal(2, _registered.Count);
    }

    [Fact]
    public async Task StartAsync_RegistersImportOffJobDailyAtThree()
    {
        GivenManagerAccepts();

        await CreateService().StartAsync(CancellationToken.None);

        var job = Registered(IJobMonitoringService.ImportOffJobName);
        Assert.Equal("0 3 * * *", job.Cron);
        Assert.Equal(typeof(IOffImportJob), job.Job.Type);
        Assert.Equal(nameof(IOffImportJob.RunAsync), job.Job.Method.Name);
    }

    [Fact]
    public async Task StartAsync_RegistersRgpdPurgeJobDailyAtThreeThirty()
    {
        GivenManagerAccepts();

        await CreateService().StartAsync(CancellationToken.None);

        var job = Registered(IJobMonitoringService.RgpdPurgeJobName);
        Assert.Equal("30 3 * * *", job.Cron);
        Assert.Equal(typeof(IRgpdPurgeJob), job.Job.Type);
        Assert.Equal(nameof(IRgpdPurgeJob.RunAsync), job.Job.Method.Name);
    }

    [Fact]
    public async Task StartAsync_SchedulesEveryJobInUtc()
    {
        GivenManagerAccepts();

        await CreateService().StartAsync(CancellationToken.None);

        // Sans cela, l'heure de purge suivrait le fuseau du serveur — donc l'heure d'été.
        Assert.All(_registered, r => Assert.Equal(TimeZoneInfo.Utc, r.Options.TimeZone));
    }

    [Fact]
    public async Task StartAsync_SchedulesPurgeAfterImport()
    {
        GivenManagerAccepts();

        await CreateService().StartAsync(CancellationToken.None);

        // La purge est décalée d'une demi-heure pour ne pas concurrencer l'écriture de l'import.
        Assert.Equal("0 3 * * *", Registered(IJobMonitoringService.ImportOffJobName).Cron);
        Assert.Equal("30 3 * * *", Registered(IJobMonitoringService.RgpdPurgeJobName).Cron);
    }

    // ── Storage indisponible ──────────────────────────────────────────────────

    [Fact]
    public async Task StartAsync_DoesNotThrowWhenStorageUnavailable()
    {
        GivenStorageUnavailable();

        // Le démarrage de l'API ne doit pas dépendre de la disponibilité du storage Hangfire :
        // le trafic HTTP n'en a pas besoin.
        await CreateService().StartAsync(CancellationToken.None);
    }

    [Fact]
    public async Task StartAsync_LogsErrorWhenStorageUnavailable()
    {
        GivenStorageUnavailable();

        await CreateService().StartAsync(CancellationToken.None);

        // La panne devient silencieuse si elle n'est pas journalisée — c'est la contrepartie
        // assumée du démarrage tolérant.
        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    // ── Arrêt ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task StopAsync_DoesNothing()
    {
        // Rien à défaire : l'enregistrement vit dans le storage, pas dans le process.
        await CreateService().StopAsync(CancellationToken.None);

        _recurringJobs.VerifyNoOtherCalls();
    }
}
