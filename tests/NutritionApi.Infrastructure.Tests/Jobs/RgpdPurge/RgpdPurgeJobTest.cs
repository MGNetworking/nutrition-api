namespace NutritionApi.Infrastructure.Tests.Jobs.RgpdPurge;

using Microsoft.Extensions.Logging;
using Moq;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Infrastructure.Jobs.RgpdPurge;

[Trait("Level", "1")]
public class RgpdPurgeJobTest
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 3, 30, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _users = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWork = new(MockBehavior.Strict);
    private readonly Mock<IKeycloakAdminService> _keycloak = new(MockBehavior.Strict);
    private readonly Mock<ILogger<RgpdPurgeJob>> _logger = new();

    /// <summary>Trace l'ordre réel des effets — l'ordre Keycloak puis PostgreSQL est un invariant.</summary>
    private readonly List<string> _steps = [];

    private RgpdPurgeJob CreateJob()
        => new(_users.Object, _unitOfWork.Object, _keycloak.Object, new FixedTimeProvider(Now), _logger.Object);

    private static User CreateUser(string keycloakId)
        => new(
            keycloakId: keycloakId,
            birthDate: DateOnly.FromDateTime(Now.UtcDateTime.AddYears(-30)),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            height: 180,
            allergies: [],
            dietaryPreferences: []);

    private void GivenExpiredUsers(params User[] users)
        => _users.Setup(r => r.GetExpiredForPurgeAsync(It.IsAny<DateTime>())).ReturnsAsync(users);

    /// <summary>Arme le trio d'appels attendu pour une purge nominale, en traçant chaque étape.</summary>
    private void GivenPurgeSucceeds()
    {
        _keycloak.Setup(k => k.DeleteUserAsync(It.IsAny<string>()))
                 .Callback<string>(id => _steps.Add($"keycloak:{id}"))
                 .Returns(Task.CompletedTask);

        _users.Setup(r => r.DeleteAsync(It.IsAny<User>()))
              .Callback<User>(u => _steps.Add($"db:{u.KeycloakId}"))
              .Returns(Task.CompletedTask);

        _unitOfWork.Setup(u => u.SaveChangesAsync())
                   .Callback(() => _steps.Add("save"))
                   .Returns(Task.CompletedTask);
    }

    // ── Chemin nominal ────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_SelectsAccountsExpiredBeyondThirtyDays()
    {
        DateTime? captured = null;
        _users.Setup(r => r.GetExpiredForPurgeAsync(It.IsAny<DateTime>()))
              .Callback<DateTime>(limit => captured = limit)
              .ReturnsAsync([]);

        await CreateJob().RunAsync();

        Assert.Equal(Now.UtcDateTime.AddDays(-30), captured);
    }

    [Fact]
    public async Task RunAsync_DeletesKeycloakAccountThenDatabaseRows()
    {
        GivenExpiredUsers(CreateUser("kc-1"));
        GivenPurgeSucceeds();

        await CreateJob().RunAsync();

        Assert.Equal(["keycloak:kc-1", "db:kc-1", "save"], _steps);
    }

    [Fact]
    public async Task RunAsync_PurgesEveryExpiredAccount()
    {
        GivenExpiredUsers(CreateUser("kc-1"), CreateUser("kc-2"));
        GivenPurgeSucceeds();

        await CreateJob().RunAsync();

        _keycloak.Verify(k => k.DeleteUserAsync("kc-1"), Times.Once);
        _keycloak.Verify(k => k.DeleteUserAsync("kc-2"), Times.Once);
        _users.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Exactly(2));
    }

    [Fact]
    public async Task RunAsync_CommitsPerUser()
    {
        GivenExpiredUsers(CreateUser("kc-1"), CreateUser("kc-2"));
        GivenPurgeSucceeds();

        await CreateJob().RunAsync();

        // Un commit par compte : l'échec du suivant ne remet pas en cause les purges déjà acquises.
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
    }

    // ── Cas limite ────────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_DoesNothingWhenNoAccountExpired()
    {
        GivenExpiredUsers();

        await CreateJob().RunAsync();

        _keycloak.Verify(k => k.DeleteUserAsync(It.IsAny<string>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    // ── Cas d'erreur ──────────────────────────────────────────────────────────

    [Fact]
    public async Task RunAsync_KeepsDatabaseIntactWhenKeycloakFails()
    {
        GivenExpiredUsers(CreateUser("kc-1"));
        _keycloak.Setup(k => k.DeleteUserAsync("kc-1")).ThrowsAsync(new HttpRequestException("keycloak down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob().RunAsync());

        // Aucune suppression partielle : le compte reste purgeable au prochain passage.
        _users.Verify(r => r.DeleteAsync(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ContinuesWithOtherAccountsWhenOneFails()
    {
        GivenExpiredUsers(CreateUser("kc-ko"), CreateUser("kc-ok"));
        GivenPurgeSucceeds();
        _keycloak.Setup(k => k.DeleteUserAsync("kc-ko")).ThrowsAsync(new HttpRequestException("keycloak down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob().RunAsync());

        _keycloak.Verify(k => k.DeleteUserAsync("kc-ok"), Times.Once);
        Assert.Contains("db:kc-ok", _steps);
        Assert.DoesNotContain("db:kc-ko", _steps);
    }

    [Fact]
    public async Task RunAsync_ThrowsSoHangfireRetriesWhenAnyAccountFailed()
    {
        GivenExpiredUsers(CreateUser("kc-1"));
        _keycloak.Setup(k => k.DeleteUserAsync("kc-1")).ThrowsAsync(new HttpRequestException("keycloak down"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob().RunAsync());

        Assert.Contains("1", exception.Message);
    }

    [Fact]
    public async Task RunAsync_LogsErrorForEachFailedAccount()
    {
        GivenExpiredUsers(CreateUser("kc-1"));
        _keycloak.Setup(k => k.DeleteUserAsync("kc-1")).ThrowsAsync(new HttpRequestException("keycloak down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateJob().RunAsync());

        _logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.Once);
    }

    /// <summary>Horloge figée — la fenêtre de grace period doit être vérifiable sans dépendre de l'heure réelle.</summary>
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
