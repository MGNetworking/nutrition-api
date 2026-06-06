namespace NutritionApi.Api.Tests;

using Microsoft.AspNetCore.Http;
using NutritionApi.Api.Middleware;
using NutritionApi.Application.Exceptions;

public class ExceptionMiddlewareTest
{
    private readonly ExceptionMiddleware _middleware = new();

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
}
