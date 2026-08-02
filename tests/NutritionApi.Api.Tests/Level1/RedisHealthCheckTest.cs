namespace NutritionApi.Api.Tests.Level1;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NutritionApi.Api.HealthChecks;
using StackExchange.Redis;

/// <summary>
/// Comportement de la sonde du cache — et surtout, ce qu'elle ne fait jamais.
/// </summary>
/// <remarks>
/// Le fil conducteur de ces cas : <b>aucun ne rend l'instance inapte</b>. Redis est un accélérateur,
/// la recherche d'aliments retombe sur PostgreSQL quand il manque. Une sonde qui répondrait
/// <see cref="HealthStatus.Unhealthy"/> transformerait une dégradation en panne — l'orchestrateur
/// retirerait du service des instances qui répondent correctement, juste plus lentement.
/// <para>
/// Le niveau 3 éprouve déjà le cas du conteneur arrêté. Restent les chemins qu'aucune coupure ne
/// produit : un multiplexeur qui lève, et un cache qui accepte la connexion sans jamais répondre.
/// </para>
/// </remarks>
[Trait("Level", "1")]
public class RedisHealthCheckTest
{
    /// <summary>Délai court : la branche du renoncement se vérifie en millisecondes.</summary>
    private static readonly TimeSpan DelaiCourt = TimeSpan.FromMilliseconds(50);

    /// <summary>cache injoignable : dégradé, jamais inapte.</summary>
    [Fact]
    public async Task CheckHealthAsync_ShouldDegrade_WhenCacheIsDisconnected()
    {
        var redis = new Mock<IConnectionMultiplexer>();
        redis.SetupGet(r => r.IsConnected).Returns(false);

        var resultat = await new RedisHealthCheck(redis.Object, NullLogger<RedisHealthCheck>.Instance, DelaiCourt)
            .CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, resultat.Status);
        Assert.Contains("PostgreSQL", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>
    /// cache connecté mais muet : la sonde renonce au bout du délai, et dégrade.
    /// </summary>
    /// <remarks>
    /// Un multiplexeur peut se croire connecté alors que le serveur ne répond plus — connexion TCP
    /// établie, commandes sans réponse. Sans délai, la sonde pendrait et l'orchestrateur n'apprendrait
    /// rien.
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_ShouldDegrade_WhenCacheNeverAnswers()
    {
        var database = new Mock<IDatabase>();
        database
            .Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
            .Returns(async () =>
            {
                await Task.Delay(System.Threading.Timeout.Infinite);
                return TimeSpan.Zero;
            });

        var resultat = await CreateSonde(database).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, resultat.Status);
        Assert.Contains("sans réponse", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>multiplexeur en erreur : dégradé, et l'exception n'échappe pas.</summary>
    /// <remarks>
    /// Une exception qui remonterait ferait échouer tout le rapport de santé, donc l'aptitude entière
    /// — exactement ce que cette sonde doit éviter.
    /// </remarks>
    [Fact]
    public async Task CheckHealthAsync_ShouldDegrade_WhenMultiplexerThrows()
    {
        var database = new Mock<IDatabase>();
        database
            .Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.SocketFailure, "socket fermé"));

        var resultat = await CreateSonde(database).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, resultat.Status);
        Assert.Contains("en erreur", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>cache joignable : sain, avec la latence mesurée.</summary>
    [Fact]
    public async Task CheckHealthAsync_ShouldSucceed_WhenCacheAnswers()
    {
        var database = new Mock<IDatabase>();
        database
            .Setup(d => d.PingAsync(It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromMilliseconds(4));

        var resultat = await CreateSonde(database).CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, resultat.Status);
        Assert.Contains("joignable", resultat.Description, StringComparison.Ordinal);
    }

    /// <summary>Construit la sonde autour d'un multiplexeur connecté servant la base donnée.</summary>
    private static RedisHealthCheck CreateSonde(Mock<IDatabase> database)
    {
        var redis = new Mock<IConnectionMultiplexer>();

        redis.SetupGet(r => r.IsConnected).Returns(true);
        redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);

        return new RedisHealthCheck(redis.Object, NullLogger<RedisHealthCheck>.Instance, DelaiCourt);
    }
}
