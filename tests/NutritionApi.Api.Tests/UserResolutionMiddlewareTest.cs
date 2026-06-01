namespace NutritionApi.Api.Tests;

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using NutritionApi.Api.Middleware;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

public class UserResolutionMiddlewareTest
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly UserResolutionMiddleware _middleware;

    public UserResolutionMiddlewareTest()
    {
        _middleware = new UserResolutionMiddleware(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUserFound_StoresUserIdInItems()
    {
        var user = CreateUser("keycloak-123");
        _userRepositoryMock.Setup(r => r.GetByKeycloakIdAsync("keycloak-123")).ReturnsAsync(user);
        var context = CreateAuthenticatedContext("keycloak-123");
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(user.Id, context.Items["UserId"]);
        Assert.True(called);
    }

    [Fact]
    public async Task InvokeAsync_AuthenticatedUserNotFound_Returns401()
    {
        _userRepositoryMock.Setup(r => r.GetByKeycloakIdAsync("keycloak-unknown")).ReturnsAsync((User?)null);
        var context = CreateAuthenticatedContext("keycloak-unknown");
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        await _middleware.InvokeAsync(context, next);

        Assert.Equal(401, context.Response.StatusCode);
        Assert.False(called);
    }

    [Fact]
    public async Task InvokeAsync_UnauthenticatedUser_CallsNextWithoutLookup()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var called = false;
        RequestDelegate next = _ => { called = true; return Task.CompletedTask; };

        await _middleware.InvokeAsync(context, next);

        Assert.True(called);
        _userRepositoryMock.Verify(r => r.GetByKeycloakIdAsync(It.IsAny<string>()), Times.Never);
    }

    private static DefaultHttpContext CreateAuthenticatedContext(string keycloakId)
    {
        var claims = new[] { new Claim("sub", keycloakId) };
        var identity = new ClaimsIdentity(claims, "Bearer");
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(identity);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static User CreateUser(string keycloakId) => new(
        keycloakId,
        new DateOnly(1990, 1, 1),
        Gender.Male,
        ActivityLevel.ModeratelyActive,
        175f,
        [],
        []);
}
