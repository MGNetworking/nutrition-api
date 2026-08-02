namespace NutritionApi.Api.Startup;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

/// <summary>
/// Précharge les clés de signature du realm au démarrage, sans jamais empêcher l'application de
/// démarrer.
/// </summary>
/// <remarks>
/// La validation des jetons est locale : l'API vérifie les signatures avec les clés publiques du
/// serveur d'identité, mises en cache. La récupération est paresseuse par défaut — au premier appel
/// authentifié, pas au démarrage. Ce service la force, pour que la première requête ne la paie pas.
/// <para>
/// <b>Il interrompait le démarrage jusqu'au 2026-08-02</b> (NTR-173). Le raisonnement d'origine était
/// juste — une instance sans clés accepte le trafic et refuse tous les jetons — mais le remède
/// coûtait plus que le mal. Un conteneur qui sort en erreur est relancé par le kubelet, et les échecs
/// répétés mènent à un <c>CrashLoopBackOff</c> dont le délai double jusqu'à cinq minutes. Un serveur
/// d'identité en retard de quatre minutes rendait l'API indisponible bien plus longtemps que lui.
/// </para>
/// <para>
/// Ce que le garde-fou cherchait à empêcher est désormais tenu par la sonde d'aptitude
/// <c>/health/ready</c> : une instance sans clés répond 503, l'orchestrateur la retire du service, et
/// elle attend — sans redémarrage — jusqu'à en obtenir. Elle y rejoint le service d'elle-même. Les
/// instances déjà pourvues, elles, continuent de servir même serveur d'identité à terre.
/// </para>
/// <para>
/// L'attente reste **bornée** : dans un cluster, l'API et le serveur d'identité démarrent souvent
/// ensemble, et quelques secondes de décalage ne justifient pas d'abandonner le préchargement. Passé
/// le délai, le service renonce et journalise ; il ne lève pas.
/// </para>
/// <para>
/// À ne pas confondre avec <see cref="KeycloakAdminConfigurationValidator"/>, qui interrompt bel et
/// bien le démarrage : une clé de configuration absente est une erreur de déploiement, elle ne se
/// répare pas d'elle-même. Une indisponibilité, si.
/// </para>
/// <para>
/// Implémenté en <see cref="IHostedService"/> à dessein : les tests de niveau 2, qui pointent vers
/// une autorité factice, retirent tous les services hébergés et ne sont donc pas concernés.
/// </para>
/// </remarks>
public sealed class KeycloakAvailabilityService : IHostedService
{
    /// <summary>Clé de configuration du délai d'attente, en secondes.</summary>
    public const string TimeoutSettingKey = "Keycloak:StartupTimeoutSeconds";

    /// <summary>Délai retenu si la configuration ne le précise pas.</summary>
    public const int DefaultTimeoutSeconds = 60;

    /// <summary>Pause entre deux tentatives.</summary>
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    private readonly IOptionsMonitor<JwtBearerOptions> _jwtOptions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeycloakAvailabilityService> _logger;

    /// <summary>Construit le service de vérification.</summary>
    /// <param name="jwtOptions">Options du schéma JWT — porte le gestionnaire de configuration OIDC.</param>
    /// <param name="configuration">Configuration de l'application.</param>
    /// <param name="logger">Journal des tentatives et de l'échec final.</param>
    public KeycloakAvailabilityService(
        IOptionsMonitor<JwtBearerOptions> jwtOptions,
        IConfiguration configuration,
        ILogger<KeycloakAvailabilityService> logger)
    {
        _jwtOptions = jwtOptions;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Récupère les clés du realm, en réessayant jusqu'au délai imparti. Ne lève jamais : un échec
    /// est journalisé et laissé à la sonde d'aptitude, qui tient l'instance hors du service.
    /// </summary>
    /// <param name="cancellationToken">Jeton d'annulation du démarrage.</param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);

        var manager = options.ConfigurationManager;

        if (manager is null)
        {
            _logger.LogError(
                "Aucun gestionnaire de configuration OIDC : la clé Keycloak:Authority est probablement "
                + "absente. Aucun jeton ne pourra être validé — /health/ready restera négatif.");

            return;
        }

        var timeout = TimeSpan.FromSeconds(
            _configuration.GetValue(TimeoutSettingKey, DefaultTimeoutSeconds));

        var deadline = DateTime.UtcNow + timeout;
        Exception? dernierEchec = null;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var configuration = await manager.GetConfigurationAsync(cancellationToken);

                if (configuration.SigningKeys.Count > 0)
                {
                    _logger.LogInformation(
                        "Clés de signature du realm récupérées ({Count}) — la validation des jetons est opérationnelle.",
                        configuration.SigningKeys.Count);

                    return;
                }

                dernierEchec = new InvalidOperationException(
                    "Le serveur d'identité a répondu, mais n'a publié aucune clé de signature.");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                dernierEchec = exception;
            }

            if (DateTime.UtcNow >= deadline)
            {
                _logger.LogError(
                    dernierEchec,
                    "Les clés de signature du realm n'ont pas pu être récupérées en {Timeout} s depuis "
                    + "« {Authority} ». L'application démarre tout de même : elle se déclarera non prête "
                    + "sur /health/ready et ne recevra aucun trafic tant qu'elle n'aura pas de clés.",
                    timeout.TotalSeconds,
                    options.Authority);

                return;
            }

            _logger.LogWarning(
                dernierEchec,
                "Serveur d'identité injoignable — nouvelle tentative dans {Delay} s.",
                RetryDelay.TotalSeconds);

            // Sans cela, le gestionnaire peut resservir son échec mémorisé au lieu de retenter
            // réellement l'appel réseau.
            manager.RequestRefresh();

            await Task.Delay(RetryDelay, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
