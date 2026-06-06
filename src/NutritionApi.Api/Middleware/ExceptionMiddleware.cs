using Microsoft.AspNetCore.Mvc;
using NutritionApi.Application.Exceptions;

namespace NutritionApi.Api.Middleware
{
    public class ExceptionMiddleware : IMiddleware
    {
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
            catch (Exception) { await WriteProblem(context, 500, "Une erreur interne est survenue."); }
        }
    }

}
