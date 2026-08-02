namespace NutritionApi.Api.Tests.Level1;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NutritionApi.Api.Middleware;
using NutritionApi.Application.Exceptions;
using System.Diagnostics;

[Trait("Level", "1")]
public class ExceptionMiddlewareTest
{
    private readonly ExceptionMiddleware _middleware = new(NullLogger<ExceptionMiddleware>.Instance);

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_NotFoundException_Returns404()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new NotFoundException("not found");

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(404, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ConflictException_Returns409()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ConflictException("conflict");

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ForbiddenException_Returns403()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ForbiddenException("forbidden");

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnprocessableException_Returns422()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new UnprocessableException("unprocessable");

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(422, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_Returns500()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new Exception("unexpected");

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        var context = CreateContext();
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        await _middleware.InvokeAsync(context, next);

        Assert.True(called);
    }

    // ── NTR-135 — invariants de domaine ───────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_ArgumentException_Returns422()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ArgumentException("macros must sum to 100");

        await _middleware.InvokeAsync(context, next);

        // Une saisie qui viole un invariant du domaine est une erreur du client, pas du serveur.
        Assert.Equal(422, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentOutOfRangeException_Returns422()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ArgumentOutOfRangeException("weight");

        await _middleware.InvokeAsync(context, next);

        // Dérivée d'ArgumentException : un seul catch doit les couvrir toutes.
        Assert.Equal(422, context.Response.StatusCode);
    }

    // ── NTR-135 — dépendance indisponible ─────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_ServiceUnavailableException_Returns503()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ServiceUnavailableException("PostgreSQL");

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(503, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ServiceUnavailableException_SetsRetryAfterHeader()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ServiceUnavailableException("PostgreSQL");

        await _middleware.InvokeAsync(context, next);

        // Sans cet en-tête, le client ne sait pas si l'échec est définitif ou passager.
        Assert.True(context.Response.Headers.ContainsKey("Retry-After"));
    }

    [Fact]
    public async Task InvokeAsync_ServiceUnavailableException_DoesNotLeakDependencyName()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new ServiceUnavailableException("PostgreSQL");

        await _middleware.InvokeAsync(context, next);

        // Le nom de la dépendance est une information d'architecture : elle va au journal,
        // pas au client.
        Assert.DoesNotContain("PostgreSQL", await ReadBody(context));
    }

    // ── NTR-135 — annulation par le client ────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_OperationCanceled_WhenClientAborted_DoesNotReturn500()
    {
        var context = CreateContext();
        context.RequestAborted = new CancellationToken(canceled: true);
        RequestDelegate next = _ => throw new OperationCanceledException();

        await _middleware.InvokeAsync(context, next);

        // Le client est parti : ce n'est pas une panne, et cela ne doit pas déclencher d'alerte.
        Assert.NotEqual(500, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_OperationCanceled_WhenClientStillConnected_Returns500()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new OperationCanceledException();

        await _middleware.InvokeAsync(context, next);

        // Annulation sans départ du client : c'est un délai dépassé interne, donc un vrai incident.
        // La distinction est portée par la clause when du middleware.
        Assert.Equal(500, context.Response.StatusCode);
    }

    // ── NTR-135 — contenu du ProblemDetails ───────────────────────────────────

    [Fact]
    public async Task InvokeAsync_WhenNoTraceIsUnderway_FallsBackToTheKestrelIdentifier()
    {
        var context = CreateContext();
        context.TraceIdentifier = "trace-de-test";
        RequestDelegate next = _ => throw new NotFoundException("introuvable");

        await _middleware.InvokeAsync(context, next);

        // Observabilité désactivée : mieux vaut un identifiant qui ne mène qu'aux journaux que pas
        // d'identifiant du tout.
        Assert.Contains("trace-de-test", await ReadBody(context));
    }

    [Fact]
    public async Task InvokeAsync_ProblemDetails_CarriesTheTraceIdAndNotTheKestrelIdentifier()
    {
        var context = CreateContext();
        context.TraceIdentifier = "identifiant-kestrel";
        RequestDelegate next = _ => throw new NotFoundException("introuvable");

        using var activite = new Activity("requete-de-test").Start();

        await _middleware.InvokeAsync(context, next);

        var corps = await ReadBody(context);

        // Le point de jonction du volet 3 : c'est cet identifiant, et lui seul, qui figure dans les
        // traces envoyées au collecteur. L'identifiant Kestrel est propre à la connexion et
        // n'apparaît dans aucune trace — le publier menait l'utilisateur nulle part (NTR-139).
        Assert.Contains(activite.TraceId.ToString(), corps);
        Assert.DoesNotContain("identifiant-kestrel", corps);
    }

    [Fact]
    public async Task InvokeAsync_ProblemDetails_CarriesTitleAndType()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new NotFoundException("introuvable");

        await _middleware.InvokeAsync(context, next);

        var corps = await ReadBody(context);
        Assert.Contains("\"title\"", corps);
        Assert.Contains("\"type\"", corps);
    }

    [Fact]
    public async Task InvokeAsync_UnhandledException_DoesNotLeakMessage()
    {
        var context = CreateContext();
        RequestDelegate next = _ => throw new InvalidOperationException("chaine de connexion invalide");

        await _middleware.InvokeAsync(context, next);

        Assert.DoesNotContain("chaine de connexion", await ReadBody(context));
    }

    /// <summary>Relit le corps de réponse écrit par le middleware.</summary>
    private static async Task<string> ReadBody(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return await new StreamReader(context.Response.Body).ReadToEndAsync();
    }
}
