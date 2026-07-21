namespace NutritionApi.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Infrastructure.Persistence;
using NutritionApi.Infrastructure.Persistence.Repositories;

public static class InfrastructureExtensions
{
    /// <summary>Enregistre le contexte EF Core, l'unité de travail et les repositories.</summary>
    /// <param name="services">Collection de services de l'application.</param>
    /// <param name="configuration">Configuration — la chaîne <c>DefaultConnection</c> est requise.</param>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        // L'unité de travail est le DbContext lui-même — même instance dans la portée de la requête
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IDietPlanRepository, DietPlanRepository>();
        services.AddScoped<IDietRepository, DietRepository>();
        services.AddScoped<IMealRepository, MealRepository>();
        services.AddScoped<IFoodItemRepository, FoodItemRepository>();
        services.AddScoped<IWeightEntryRepository, WeightEntryRepository>();
        services.AddScoped<ISavedFoodItemRepository, SavedFoodItemRepository>();

        return services;
    }
}
