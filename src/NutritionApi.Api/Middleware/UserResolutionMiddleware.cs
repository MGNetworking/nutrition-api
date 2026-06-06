using NutritionApi.Application.Interfaces.Repositories;
using System.Security.Claims;

namespace NutritionApi.Api.Middleware;

public class UserResolutionMiddleware : IMiddleware
{
    private readonly IUserRepository _userRepository;

    public UserResolutionMiddleware(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var keycloakId = context.User.FindFirstValue("sub");
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId!);
            if (user is null) { context.Response.StatusCode = 401; return; }
            context.Items["UserId"] = user.Id;
        }
        await next(context);
    }


}
