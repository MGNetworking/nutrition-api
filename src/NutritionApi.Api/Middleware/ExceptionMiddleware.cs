using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.Exceptions;

namespace NutritionApi.Api.Middleware
{
    /// <summary>
    /// Traduit toute exception non gérée en réponse HTTP normalisée (RFC 9457).
    /// </summary>
    /// <remarks>
    /// Le middleware ne connaît que des abstractions d'Application : c'est Infrastructure qui
    /// convertit ses échecs techniques en <see cref="ServiceUnavailableException"/>. Sans cela,
    /// la couche API devrait référencer Npgsql et StackExchange.Redis.
    /// </remarks>
    public class ExceptionMiddleware : IMiddleware
    {
        /// <summary>Délai suggéré au client avant nouvelle tentative, en secondes.</summary>
        private const string RetryAfterSeconds = "10";

        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Retourne l'URI de spécification du statut. La RFC 9457 en fait l'identifiant stable du
        /// type de problème — c'est sur lui qu'un client généré s'appuie, plutôt que sur le libellé.
        /// </summary>
        /// <param name="statusCode">Statut HTTP renvoyé.</param>
        /// <returns>L'URI décrivant ce statut.</returns>
        private static string TypeUri(int statusCode) => statusCode switch
        {
            403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
            404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
            409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
            503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
            _   => "https://tools.ietf.org/html/rfc9110#section-15.6.1"
        };

        /// <summary>Écrit la réponse d'erreur normalisée.</summary>
        /// <param name="context">Contexte de la requête.</param>
        /// <param name="statusCode">Statut HTTP à renvoyer.</param>
        /// <param name="title">Libellé stable de la catégorie d'erreur.</param>
        /// <param name="detail">Message destiné au client — jamais un détail d'infrastructure.</param>
        private static async Task WriteProblem(HttpContext context, int statusCode, string title, string detail)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = TypeUri(statusCode),

                // Sans identifiant, un utilisateur signalant une erreur ne laisse aucun moyen de
                // retrouver l'entrée de journal correspondante — ce qui pousse à rendre les messages
                // plus bavards, l'inverse du but recherché. Un identifiant opaque n'a aucune valeur
                // pour un attaquant.
                Extensions = { ["traceId"] = context.TraceIdentifier }
            };

            await context.Response.WriteAsJsonAsync(problem);
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }

            // ── Le client est parti ───────────────────────────────────────────
            // Ni panne ni erreur : la réponse n'a plus de destinataire. La journaliser en erreur
            // remplirait les alertes au rythme des utilisateurs impatients.
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                _logger.LogInformation("Requête annulée par le client sur {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            }

            // ── Règles métier ─────────────────────────────────────────────────
            catch (NotFoundException ex) { await ClientError(context, 404, "Ressource introuvable", ex); }
            catch (ConflictException ex) { await ClientError(context, 409, "Conflit", ex); }
            catch (ForbiddenException ex) { await ClientError(context, 403, "Accès refusé", ex); }
            catch (UnprocessableException ex) { await ClientError(context, 422, "Requête inexploitable", ex); }

            // ── Invariants du domaine ─────────────────────────────────────────
            // Par convention du projet, ArgumentException signale une donnée invalide, pas un défaut
            // interne. Le message vient du domaine et décrit la règle violée : il est utile au client.
            catch (ArgumentException ex) { await ClientError(context, 422, "Donnée invalide", ex); }

            // ── Dépendance injoignable ────────────────────────────────────────
            catch (ServiceUnavailableException ex)
            {
                _logger.LogError(ex, "Dépendance indisponible : {Dependency} sur {Method} {Path}",
                    ex.Dependency, context.Request.Method, context.Request.Path);

                context.Response.Headers.RetryAfter = RetryAfterSeconds;
                await WriteProblem(context, 503, "Service momentanément indisponible",
                    "Le service est temporairement indisponible. Merci de réessayer dans quelques instants.");
            }

            // ── Tout le reste ─────────────────────────────────────────────────
            catch (Exception ex)
            {
                // Une 500 sans trace est indiagnosticable en production : on journalise l'exception
                // complète, le client ne reçoit qu'un message générique.
                _logger.LogError(ex, "Exception non gérée sur {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await WriteProblem(context, 500, "Erreur interne", "Une erreur interne est survenue.");
            }
        }

        /// <summary>
        /// Traite une erreur imputable au client : le message d'origine lui est renvoyé, et
        /// l'incident est journalisé en avertissement — sans trace, puisqu'il ne révèle aucun défaut.
        /// </summary>
        /// <param name="context">Contexte de la requête.</param>
        /// <param name="statusCode">Statut HTTP à renvoyer.</param>
        /// <param name="title">Libellé de la catégorie d'erreur.</param>
        /// <param name="exception">Exception à l'origine du refus.</param>
        private async Task ClientError(HttpContext context, int statusCode, string title, Exception exception)
        {
            _logger.LogWarning("{StatusCode} sur {Method} {Path} — {Message}",
                statusCode, context.Request.Method, context.Request.Path, exception.Message);

            await WriteProblem(context, statusCode, title, exception.Message);
        }
    }
}
