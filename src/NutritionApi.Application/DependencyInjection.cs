namespace NutritionApi.Application;

using Microsoft.Extensions.DependencyInjection;
using Interfaces.Services;
using Services;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        return services;
    }
}
