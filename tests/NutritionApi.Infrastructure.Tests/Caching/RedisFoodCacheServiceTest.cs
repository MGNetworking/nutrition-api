namespace NutritionApi.Infrastructure.Tests.Caching;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Domain.Enums;
using NutritionApi.Infrastructure.Caching;
using NutritionApi.Infrastructure.Observability;
using StackExchange.Redis;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.Json;

[Trait("Level", "1")]
public class RedisFoodCacheServiceTest
{
    private const string PouletKey = "food:search:v1:poulet";
    private const string InstrumentDesRecherches = "nutrition.cache.lookups";

    private readonly Mock<IConnectionMultiplexer> _redis = new(MockBehavior.Strict);
    private readonly Mock<IDatabase> _db = new(MockBehavior.Strict);
    private readonly Mock<IServer> _server = new(MockBehavior.Strict);
    private readonly Mock<ILogger<RedisFoodCacheService>> _logger = new();

    private readonly IMeterFactory _fabrique =
        new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();

    private static readonly List<FoodItemSearchResponse> Poulet =
    [
        new(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Poulet", 165f, 31f, 0f, 3.6f, [Allergen.Milk])
    ];

    private RedisFoodCacheService CreateService(int? ttlHours = null)
    {
        var settings = new Dictionary<string, string?>();

        if (ttlHours is not null)
            settings["Redis:SearchCacheTtlHours"] = ttlHours.Value.ToString();

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        return new RedisFoodCacheService(
            _redis.Object, configuration, _logger.Object, new InfrastructureMetrics(_fabrique));
    }

    /// <summary>Branche un collecteur sur le compteur des recherches présentées au cache.</summary>
    private MetricCollector<long> ObserverLesRecherches()
        => new(_fabrique, InfrastructureMetrics.MeterName, InstrumentDesRecherches);

    /// <summary>Branche le multiplexeur sur la base mockée — commun à toutes les commandes de données.</summary>
    private void GivenDatabase()
        => _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_db.Object);

    /// <summary>Vérifie qu'une dégradation a bien été journalisée — elle ne doit jamais être silencieuse.</summary>
    private void VerifyWarningLogged(Times times)
        => _logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);

    private static RedisConnectionException RedisDown()
        => new(ConnectionFailureType.SocketFailure, "Redis indisponible");

    // ---------------------------------------------------------------------
    // Chemin nominal
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WhenCached_ReturnsDeserializedResults()
    {
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ReturnsAsync(JsonSerializer.Serialize(Poulet));

        var result = await CreateService().GetAsync("poulet");

        Assert.NotNull(result);
        var item = Assert.Single(result);
        Assert.Equal("Poulet", item.Name);
        Assert.Equal(165f, item.CaloriesPer100g);
        Assert.Equal([Allergen.Milk], item.AllergensTags);
    }

    [Fact]
    public async Task GetAsync_NormalizesKeywordAndPrefixesSchemaVersion()
    {
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ReturnsAsync(RedisValue.Null);

        await CreateService().GetAsync("  Poulet  ");

        _db.Verify(d => d.StringGetAsync((RedisKey)PouletKey, It.IsAny<CommandFlags>()), Times.Once);
    }

    // ---------------------------------------------------------------------
    // Mesure du cache (NTR-138)
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WhenCached_CountsAHit()
    {
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ReturnsAsync(JsonSerializer.Serialize(Poulet));
        var releves = ObserverLesRecherches();

        await CreateService().GetAsync("poulet");

        Assert.Equal("hit", Assert.Single(releves.GetMeasurementSnapshot()).Tags["outcome"]);
    }

    [Fact]
    public async Task GetAsync_WhenAbsent_CountsAMiss()
    {
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ReturnsAsync(RedisValue.Null);
        var releves = ObserverLesRecherches();

        await CreateService().GetAsync("poulet");

        Assert.Equal("miss", Assert.Single(releves.GetMeasurementSnapshot()).Tags["outcome"]);
    }

    [Fact]
    public async Task GetAsync_WhenRedisIsDown_CountsAFailureAndNotAMiss()
    {
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ThrowsAsync(RedisDown());
        var releves = ObserverLesRecherches();

        await CreateService().GetAsync("poulet");

        // L'appelant reçoit null dans les deux cas et repart en base. Les confondre ferait passer
        // un cache injoignable pour un cache qui ne sert jamais — deux pannes très différentes.
        Assert.Equal("failure", Assert.Single(releves.GetMeasurementSnapshot()).Tags["outcome"]);
    }

    [Fact]
    public async Task SetAsync_WritesWithConfiguredTtl()
    {
        GivenDatabase();
        _db.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>()))
           .ReturnsAsync(true);

        await CreateService(ttlHours: 6).SetAsync("Poulet", Poulet);

        _db.Verify(d => d.StringSetAsync(
            (RedisKey)PouletKey,
            It.IsAny<RedisValue>(),
            (Expiration)TimeSpan.FromHours(6)), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WhenTtlNotConfigured_UsesDefaultOf24Hours()
    {
        GivenDatabase();
        _db.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>()))
           .ReturnsAsync(true);

        await CreateService().SetAsync("poulet", Poulet);

        _db.Verify(d => d.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            (Expiration)TimeSpan.FromHours(24)), Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_DeletesTheKey()
    {
        GivenDatabase();
        _db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        await CreateService().InvalidateAsync("poulet");

        _db.Verify(d => d.KeyDeleteAsync((RedisKey)PouletKey, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task InvalidateAllSearchesAsync_DeletesEveryVersionOfSearchKeys()
    {
        // Le motif d'invalidation ignore la version de schéma : une entrée écrite par une version
        // antérieure doit disparaître elle aussi, sans quoi elle survivrait jusqu'à son expiration.
        RedisKey[] keys = ["food:search:v1:poulet", "food:search:riz"];
        GivenSearchKeysOnServer(keys);
        _db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);

        await CreateService().InvalidateAllSearchesAsync();

        _db.Verify(d => d.KeyDeleteAsync((RedisKey)"food:search:v1:poulet", It.IsAny<CommandFlags>()), Times.Once);
        _db.Verify(d => d.KeyDeleteAsync((RedisKey)"food:search:riz", It.IsAny<CommandFlags>()), Times.Once);
    }

    // ---------------------------------------------------------------------
    // Cas limites
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WhenKeyAbsent_ReturnsNull()
    {
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ReturnsAsync(RedisValue.Null);

        Assert.Null(await CreateService().GetAsync("poulet"));
    }

    [Fact]
    public async Task InvalidateAllSearchesAsync_WhenNoKeyMatches_DeletesNothing()
    {
        GivenSearchKeysOnServer([]);

        await CreateService().InvalidateAllSearchesAsync();

        _db.Verify(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    // ---------------------------------------------------------------------
    // Cas d'erreur
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetAsync_WhenRedisUnavailable_ReturnsNullAndLogsWarning()
    {
        // Une panne du cache doit coûter de la performance, pas de la disponibilité :
        // null est déjà la valeur qui déclenche le repli sur PostgreSQL côté FoodItemService.
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ThrowsAsync(RedisDown());

        Assert.Null(await CreateService().GetAsync("poulet"));
        VerifyWarningLogged(Times.Once());
    }

    [Fact]
    public async Task SetAsync_WhenRedisUnavailable_DoesNotThrowAndLogsWarning()
    {
        GivenDatabase();
        _db.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<Expiration>()))
           .ThrowsAsync(RedisDown());

        await CreateService().SetAsync("poulet", Poulet);

        VerifyWarningLogged(Times.Once());
    }

    [Fact]
    public async Task GetAsync_WhenPayloadIsNotDeserializable_LetsTheExceptionSurface()
    {
        // Un payload illisible n'est pas une panne d'infrastructure mais un vrai défaut
        // (forme du DTO modifiée sans changement de version de schéma) : il doit remonter.
        GivenDatabase();
        _db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ReturnsAsync("{ ceci n'est pas du JSON");

        var service = CreateService();

        await Assert.ThrowsAsync<JsonException>(() => service.GetAsync("poulet"));
        VerifyWarningLogged(Times.Never());
    }

    [Fact]
    public async Task InvalidateAllSearchesAsync_ShouldLogAndContinue_WhenRedisIsUnavailable()
    {
        // Une panne du cache ne doit pas faire échouer l'import qui vient de le nettoyer : les
        // aliments sont déjà en base, seul le nettoyage manque. Laisser l'exception remonter
        // provoquait un retéléchargement du dump et le retraitement de plusieurs millions de
        // lignes. Le prix accepté est un cache périmé jusqu'à l'expiration de ses entrées.
        GivenSearchKeysOnServer(["food:search:v1:poulet"]);
        _db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ThrowsAsync(RedisDown());

        var service = CreateService();

        await service.InvalidateAllSearchesAsync();

        VerifyWarningLogged(Times.Once());
    }

    [Fact]
    public async Task InvalidateAllSearchesAsync_ShouldLogAndContinue_WhenScanTimesOut()
    {
        // RedisTimeoutException dérive de TimeoutException, pas de RedisException : un catch sur
        // cette dernière seule laissait passer le cas réel. C'est précisément ce que produit un
        // Redis arrêté quand le parcours des clés attend la réponse au SCAN — défaut trouvé par le
        // cas de niveau 3, invisible ici tant que ce test n'existait pas.
        GivenSearchKeysOnServer(["food:search:v1:poulet"]);
        _db.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
           .ThrowsAsync(new RedisTimeoutException("Timeout performing SCAN", CommandStatus.WaitingToBeSent));

        var service = CreateService();

        await service.InvalidateAllSearchesAsync();

        VerifyWarningLogged(Times.Once());
    }

    // ---------------------------------------------------------------------

    /// <summary>
    /// Prépare le parcours des clés côté serveur. Le motif attendu est figé dans le setup :
    /// avec un mock strict, tout autre motif ferait échouer le test.
    /// </summary>
    private void GivenSearchKeysOnServer(RedisKey[] keys)
    {
        var endpoint = new DnsEndPoint("localhost", 6379);

        GivenDatabase();
        _redis.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns([endpoint]);
        _redis.Setup(r => r.GetServer(endpoint, It.IsAny<object>())).Returns(_server.Object);

        _server.Setup(s => s.Keys(
            It.IsAny<int>(), (RedisValue)"food:search:*", It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        _server.Setup(s => s.Keys(
            It.IsAny<int>(), (RedisValue)"food:search:*", It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);
    }
}
