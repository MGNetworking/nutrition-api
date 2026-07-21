using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.Exceptions;

namespace NutritionApi.Api.Middleware
{
    public class ExceptionMiddleware : IMiddleware
    {
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
        {
            _logger = logger;
        }

        private static async Task WriteProblem(HttpContext context, int statusCode, string detail)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";

            var problem = new ProblemDetails
            {
                Status = statusCode,
                Detail = detail
            };

            await context.Response.WriteAsJsonAsync(problem);
        }


        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try { await next(context); }
            catch (NotFoundException ex) { await WriteProblem(context, 404, ex.Message); }
            catch (ConflictException ex) { await WriteProblem(context, 409, ex.Message); }
            catch (ForbiddenException ex) { await WriteProblem(context, 403, ex.Message); }
            catch (UnprocessableException ex) { await WriteProblem(context, 422, ex.Message); }
            catch (Exception ex)
            {
                // Une 500 sans trace est indiagnosticable en production : on journalise
                // l'exception complète, le client ne reçoit qu'un message générique.
                _logger.LogError(ex, "Exception non gérée sur {Method} {Path}",
                    context.Request.Method, context.Request.Path);
                await WriteProblem(context, 500, "Une erreur interne est survenue.");
            }
        }
    }

}
