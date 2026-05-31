namespace NutritionApi.Application;

using Microsoft.Extensions.DependencyInjection;
using Services;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection service)
    {
        service.AddScoped<UserService>();

        return service;
    }
}
