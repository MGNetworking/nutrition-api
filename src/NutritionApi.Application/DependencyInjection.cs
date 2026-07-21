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

        // IFoodItemService et IAdminService ne sont pas encore enregistrables : leurs dépendances
        // Infrastructure n'existent pas (IFoodCacheService → NTR-54, IJobMonitoringService → NTR-55).
        // Les enregistrer maintenant ferait échouer la validation du conteneur au démarrage.

        return services;
    }
}
