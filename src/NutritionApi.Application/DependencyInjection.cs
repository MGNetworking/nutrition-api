namespace NutritionApi.Application;

using Microsoft.Extensions.DependencyInjection;
using Interfaces.Services;
using Services;

public static class ApplicationExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Règles de quota et d'accès par palier d'abonnement — injectée par plusieurs services
        services.AddScoped<SubscriptionGuard>();

        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRgpdService, RgpdService>();
        services.AddScoped<IDietPlanService, DietPlansService>();
        services.AddScoped<IDietService, DietService>();
        services.AddScoped<IMealService, MealService>();
        services.AddScoped<INutritionService, NutritionService>();
        services.AddScoped<IFoodItemService, FoodItemService>();

        // IAdminService n'est pas encore enregistrable : IJobMonitoringService n'a pas
        // d'implémentation (NTR-55). L'enregistrer maintenant ferait échouer la validation
        // du conteneur au démarrage.

        return services;
    }
}
