namespace NutritionApi.Api.HealthChecks;

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

/// <summary>
/// Écrit le résultat des sondes en JSON, dépendance par dépendance.
/// </summary>
/// <remarks>
/// La réponse par défaut d'ASP.NET Core est le seul mot <c>Healthy</c> ou <c>Unhealthy</c>. C'est
/// assez pour l'orchestrateur, qui ne lit que le code HTTP, et insuffisant pour la personne qui
/// diagnostique : un 503 ne dit pas si c'est la base ou le serveur d'identité qui manque.
/// <para>
/// Le détail sert aussi les smoke tests de niveau 4 (NTR-112), dont l'intérêt est de désigner la
/// brique en cause, et l'observabilité (NTR-136), qui republiera ces mêmes résultats en métriques.
/// </para>
/// </remarks>
public static class HealthReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Sérialise le rapport dans la réponse HTTP.</summary>
    /// <param name="context">Contexte de la requête en cours.</param>
    /// <param name="report">Rapport produit par l'exécution des sondes.</param>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var charge = new
        {
            statut = report.Status.ToString(),
            duree = $"{report.TotalDuration.TotalMilliseconds:0} ms",
            dependances = report.Entries.ToDictionary(
                entree => entree.Key,
                entree => new
                {
                    statut = entree.Value.Status.ToString(),
                    description = entree.Value.Description,
                    duree = $"{entree.Value.Duration.TotalMilliseconds:0} ms",
                    // L'exception n'est jamais exposée : elle porte des chaînes de connexion et des
                    // adresses internes. Elle est journalisée par l'infrastructure de santé.
                    erreur = entree.Value.Exception is null ? null : entree.Value.Exception.GetType().Name
                })
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(charge, JsonOptions));
    }
}
