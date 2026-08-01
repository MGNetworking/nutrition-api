namespace NutritionApi.Api.Startup;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

/// <summary>
/// Empêche l'application de démarrer tant que les clés de signature du realm n'ont pas été
/// récupérées.
/// </summary>
/// <remarks>
/// La validation des jetons est locale : l'API vérifie les signatures avec les clés publiques du
/// serveur d'identité, mises en cache. Une instance démarrée pendant que celui-ci est indisponible
/// n'a **aucune clé** — elle accepte le trafic et refuse tous les jetons, pendant que ses voisines,
/// démarrées plus tôt, fonctionnent normalement. Deux instances derrière le même service, deux
/// comportements : c'est le pire cas à diagnostiquer.
/// <para>
/// La récupération est paresseuse par défaut — au premier appel authentifié, pas au démarrage. Ce
/// service la force, et fait échouer le démarrage si elle n'aboutit pas.
/// </para>
/// <para>
/// L'attente est **bornée** plutôt qu'immédiate : dans un cluster, l'API et le serveur d'identité
/// démarrent souvent ensemble, et quelques secondes de décalage ne justifient pas un cycle de
/// redémarrage. Passé le délai, l'échec est franc et l'orchestrateur relance l'instance.
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

    /// <summary>Récupère les clés du realm, en réessayant jusqu'au délai imparti.</summary>
    /// <param name="cancellationToken">Jeton d'annulation du démarrage.</param>
    /// <exception cref="InvalidOperationException">
    /// Les clés n'ont pas pu être récupérées dans le délai. L'hôte s'arrête : mieux vaut une instance
    /// qui refuse de démarrer qu'une instance qui rejette tous les jetons.
    /// </exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var options = _jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme);

        var manager = options.ConfigurationManager
            ?? throw new InvalidOperationException(
                "Aucun gestionnaire de configuration OIDC : la clé Keycloak:Authority est probablement absente.");

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
                throw new InvalidOperationException(
                    $"Les clés de signature du realm n'ont pas pu être récupérées en {timeout.TotalSeconds:0} s "
                    + $"depuis « {options.Authority} ». L'application ne peut pas valider de jetons : démarrage interrompu.",
                    dernierEchec);
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
