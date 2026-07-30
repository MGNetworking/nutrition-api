namespace NutritionApi.Integration.Tests.Fixtures;

using System.Diagnostics;

/// <summary>
/// Pilote les conteneurs de la pile depuis un test — arrêt, redémarrage, attente de disponibilité.
/// </summary>
/// <remarks>
/// Plusieurs cas de ce niveau coupent une dépendance pour observer la réaction de l'application. Ce
/// sont les seuls tests de la suite qui manipulent l'infrastructure : la parallélisation est
/// désactivée pour tout l'assembly, et chaque test remet l'état en place avant de rendre la main.
/// </remarks>
public static class DockerContainer
{
    /// <summary>Conteneur PostgreSQL — surchargeable en CI.</summary>
    public static string Postgres =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_PG_CONTAINER") ?? "nutrition-postgres";

    /// <summary>Conteneur Redis — surchargeable en CI.</summary>
    public static string Redis =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_REDIS_CONTAINER") ?? "nutrition-redis";

    /// <summary>Conteneur Keycloak — surchargeable en CI.</summary>
    public static string Keycloak =>
        Environment.GetEnvironmentVariable("NUTRITION_TEST_KC_CONTAINER") ?? "nutrition-keycloak";

    /// <summary>Arrête un conteneur.</summary>
    /// <param name="container">Nom du conteneur.</param>
    public static void Stop(string container) => Run("stop", container);

    /// <summary>Démarre un conteneur.</summary>
    /// <param name="container">Nom du conteneur.</param>
    public static void Start(string container) => Run("start", container);

    /// <summary>Attend qu'un conteneur repasse <c>healthy</c>.</summary>
    /// <param name="container">Nom du conteneur.</param>
    /// <param name="timeout">Délai au-delà duquel le test échoue.</param>
    /// <exception cref="InvalidOperationException">Le conteneur n'est pas redevenu sain à temps.</exception>
    /// <remarks>
    /// Même logique que <c>wait_healthy</c> de <c>scripts/lib.sh</c> : sans cette attente, les tests
    /// suivants tomberaient sur un service encore en cours de démarrage.
    /// </remarks>
    public static async Task WaitHealthyAsync(string container, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (await StatusAsync(container) == "healthy")
                return;

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        throw new InvalidOperationException(
            $"{container} n'est pas redevenu healthy en {timeout.TotalSeconds:0} s. "
            + $"Diagnostiquer avec : docker logs {container}");
    }

    /// <summary>Lit l'état de santé courant d'un conteneur.</summary>
    /// <param name="container">Nom du conteneur.</param>
    /// <returns>L'état rapporté par Docker, ou une chaîne vide s'il est indisponible.</returns>
    private static async Task<string> StatusAsync(string container)
    {
        var info = new ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true };
        info.ArgumentList.Add("inspect");
        info.ArgumentList.Add("--format={{.State.Health.Status}}");
        info.ArgumentList.Add(container);

        using var process = Process.Start(info)!;
        var status = (await process.StandardOutput.ReadToEndAsync()).Trim();
        await process.WaitForExitAsync();

        return status;
    }

    /// <summary>Exécute une commande <c>docker</c> et échoue si elle ne rend pas 0.</summary>
    /// <param name="arguments">Arguments passés à <c>docker</c>.</param>
    /// <exception cref="InvalidOperationException">Docker est absent, ou la commande a échoué.</exception>
    private static void Run(params string[] arguments)
    {
        var info = new ProcessStartInfo("docker") { RedirectStandardError = true, RedirectStandardOutput = true };

        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = Process.Start(info)
            ?? throw new InvalidOperationException("Docker est introuvable dans le PATH.");

        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"docker {string.Join(' ', arguments)} a échoué : {process.StandardError.ReadToEnd()}");
        }
    }
}
