namespace NutritionApi.Api.Startup;

using Microsoft.Extensions.Options;
using NutritionApi.Infrastructure.ExternalServices.Keycloak;

/// <summary>
/// Empêche l'application de démarrer si les paramètres de connexion à l'API d'administration
/// Keycloak sont incomplets.
/// </summary>
/// <remarks>
/// Ces valeurs sont vides dans <c>appsettings.json</c> par conception — le secret du client de
/// service ne doit jamais être versionné — et renseignées par l'environnement. Rien ne vérifiait
/// qu'elles l'étaient réellement.
/// <para>
/// L'omission ne se voit pas au démarrage : elle se paie la nuit suivante. Le seul consommateur est
/// la purge RGPD, dont le job intercepte toute exception, la journalise et poursuit. Un déploiement
/// sans secret produirait donc une purge qui échoue chaque nuit sans que rien ne l'indique, alors
/// que des comptes dont le délai de grâce est expiré doivent être supprimés du realm.
/// </para>
/// <para>
/// Le contrôle ne franchit pas le réseau : il lit la configuration déjà chargée. C'est un oubli de
/// déploiement qu'il détecte, pas une panne — celle-ci relève de
/// <see cref="KeycloakAvailabilityService"/>. D'où l'enregistrement en premier : quatre chaînes lues
/// en mémoire échouent immédiatement, là où la vérification de disponibilité attend le réseau.
/// </para>
/// <para>
/// Implémenté en <see cref="IHostedService"/> à dessein, pour la même raison que son voisin : les
/// tests de niveau 2 substituent <c>IKeycloakAdminService</c> et ne renseignent aucune de ces clés.
/// Retirant tous les services hébergés, ils ne sont pas concernés.
/// </para>
/// </remarks>
public sealed class KeycloakAdminConfigurationValidator : IHostedService
{
    private readonly KeycloakAdminOptions _options;

    /// <summary>Construit le service de contrôle.</summary>
    /// <param name="options">Paramètres de la section <c>Keycloak</c>, tels que l'environnement les a fournis.</param>
    public KeycloakAdminConfigurationValidator(IOptions<KeycloakAdminOptions> options)
        => _options = options.Value;

    /// <summary>Vérifie que les quatre paramètres nécessaires sont renseignés.</summary>
    /// <param name="cancellationToken">Jeton d'annulation du démarrage — inutilisé, le contrôle est immédiat.</param>
    /// <exception cref="InvalidOperationException">
    /// Au moins un paramètre est absent ou vide. Le message les nomme tous : sans cela, un
    /// déploiement mal configuré se corrigerait par redémarrages successifs, une clé à la fois.
    /// </exception>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        List<string> manquantes = [];

        if (string.IsNullOrWhiteSpace(_options.AdminBaseUrl))
            manquantes.Add(nameof(KeycloakAdminOptions.AdminBaseUrl));

        if (string.IsNullOrWhiteSpace(_options.Realm))
            manquantes.Add(nameof(KeycloakAdminOptions.Realm));

        if (string.IsNullOrWhiteSpace(_options.ServiceClientId))
            manquantes.Add(nameof(KeycloakAdminOptions.ServiceClientId));

        if (string.IsNullOrWhiteSpace(_options.ServiceClientSecret))
            manquantes.Add(nameof(KeycloakAdminOptions.ServiceClientSecret));

        if (manquantes.Count > 0)
        {
            var cles = string.Join(
                ", ",
                manquantes.Select(nom => $"{KeycloakAdminOptions.SectionName}:{nom}"));

            throw new InvalidOperationException(
                $"Configuration Keycloak Admin incomplète — clés absentes ou vides : {cles}. "
                + "La purge RGPD ne pourrait pas supprimer les comptes du realm, et son échec serait "
                + "silencieux : démarrage interrompu.");
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
