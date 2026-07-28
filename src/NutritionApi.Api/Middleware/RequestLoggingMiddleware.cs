using System.Diagnostics;

namespace NutritionApi.Api.Middleware;

/// <summary>Trace chaque requête HTTP : méthode, route, code de statut et durée de traitement.</summary>
public sealed class RequestLoggingMiddleware : IMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    /// <summary>Construit le middleware de journalisation.</summary>
    /// <param name="logger">Journal recevant une entrée par requête.</param>
    public RequestLoggingMiddleware(ILogger<RequestLoggingMiddleware> logger) => _logger = logger;

    /// <summary>
    /// Mesure le traitement complet de la requête et en journalise le bilan. La trace est émise
    /// même lorsque le pipeline lève : l'exception est relayée telle quelle à
    /// <see cref="ExceptionMiddleware"/>, qui reste seul responsable de la réponse d'erreur.
    /// </summary>
    /// <param name="context">Contexte de la requête en cours.</param>
    /// <param name="next">Suite du pipeline.</param>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var start = Stopwatch.GetTimestamp();

        try
        {
            await next(context);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            _logger.Log(
                LevelFor(context.Response.StatusCode),
                "HTTP {Method} {Path} -> {StatusCode} en {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                elapsed.TotalMilliseconds.ToString("F1"));
        }
    }

    /// <summary>
    /// Gradue la trace sur le statut de réponse : une requête refusée n'a pas la même valeur
    /// diagnostique qu'une requête servie, et une erreur serveur doit ressortir des deux.
    /// </summary>
    /// <param name="statusCode">Code de statut de la réponse.</param>
    /// <returns>Le niveau de journalisation correspondant.</returns>
    private static LogLevel LevelFor(int statusCode) => statusCode switch
    {
        >= 500 => LogLevel.Error,
        >= 400 => LogLevel.Warning,
        _      => LogLevel.Information
    };
}
