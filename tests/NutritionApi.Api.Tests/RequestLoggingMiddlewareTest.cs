namespace NutritionApi.Api.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NutritionApi.Api.Middleware;

public class RequestLoggingMiddlewareTest
{
    private readonly CapturingLogger _logger = new();

    private RequestLoggingMiddleware CreateMiddleware() => new(_logger);

    private static DefaultHttpContext CreateContext(string method = "GET", string path = "/api/v1/meals")
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        return context;
    }

    /// <summary>Poursuit le pipeline en fixant le statut de réponse demandé.</summary>
    private static RequestDelegate NextReturning(int statusCode)
        => context =>
        {
            context.Response.StatusCode = statusCode;
            return Task.CompletedTask;
        };

    // ── Chemin nominal ────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_CallsNext()
    {
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        await CreateMiddleware().InvokeAsync(CreateContext(), next);

        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_LogsMethodPathAndStatusCode()
    {
        await CreateMiddleware().InvokeAsync(CreateContext("POST", "/api/v1/diets"), NextReturning(201));

        var entry = Assert.Single(_logger.Entries);
        Assert.Contains("POST", entry.Message);
        Assert.Contains("/api/v1/diets", entry.Message);
        Assert.Contains("201", entry.Message);
    }

    [Fact]
    public async Task InvokeAsync_LogsElapsedTime()
    {
        await CreateMiddleware().InvokeAsync(CreateContext(), NextReturning(200));

        var entry = Assert.Single(_logger.Entries);
        Assert.Contains("ms", entry.Message);
    }

    [Fact]
    public async Task InvokeAsync_LogsOncePerRequest()
    {
        await CreateMiddleware().InvokeAsync(CreateContext(), NextReturning(200));

        Assert.Single(_logger.Entries);
    }

    // ── Niveau de journalisation selon le statut ──────────────────────────────

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(204)]
    [InlineData(304)]
    public async Task InvokeAsync_LogsInformationWhenRequestSucceeds(int statusCode)
    {
        await CreateMiddleware().InvokeAsync(CreateContext(), NextReturning(statusCode));

        Assert.Equal(LogLevel.Information, Assert.Single(_logger.Entries).Level);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(422)]
    public async Task InvokeAsync_LogsWarningWhenRequestFails(int statusCode)
    {
        await CreateMiddleware().InvokeAsync(CreateContext(), NextReturning(statusCode));

        Assert.Equal(LogLevel.Warning, Assert.Single(_logger.Entries).Level);
    }

    [Fact]
    public async Task InvokeAsync_LogsErrorWhenServerFails()
    {
        await CreateMiddleware().InvokeAsync(CreateContext(), NextReturning(500));

        Assert.Equal(LogLevel.Error, Assert.Single(_logger.Entries).Level);
    }

    // ── Cas d'erreur ──────────────────────────────────────────────────────────

    [Fact]
    public async Task InvokeAsync_LogsEvenWhenPipelineThrows()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("boum");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateMiddleware().InvokeAsync(CreateContext(), next));

        // La trace ne doit pas disparaître parce que la requête a échoué.
        Assert.Single(_logger.Entries);
    }

    [Fact]
    public async Task InvokeAsync_RethrowsSoExceptionMiddlewareCanHandleIt()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("boum");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateMiddleware().InvokeAsync(CreateContext(), next));
    }

    /// <summary>Journal de test — retient le niveau et le message rendu de chaque entrée.</summary>
    private sealed class CapturingLogger : ILogger<RequestLoggingMiddleware>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }
}
