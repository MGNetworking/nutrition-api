namespace NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>Lecture de la chaîne des causes d'une exception.</summary>
/// <remarks>
/// Utile aux cas qui éprouvent un refus de démarrage : l'hôte enveloppe l'échec du service hébergé
/// qui a levé, et le message d'origine n'est jamais celui de l'exception de premier niveau.
/// </remarks>
public static class ExceptionChain
{
    /// <summary>Concatène les messages d'une exception et de toutes ses causes.</summary>
    /// <param name="exception">Exception de tête.</param>
    /// <returns>Les messages, du plus externe au plus interne.</returns>
    public static string DeroulerLesCauses(Exception exception)
    {
        var messages = new List<string>();

        for (Exception? courante = exception; courante is not null; courante = courante.InnerException)
            messages.Add(courante.Message);

        return string.Join(" | ", messages);
    }
}
