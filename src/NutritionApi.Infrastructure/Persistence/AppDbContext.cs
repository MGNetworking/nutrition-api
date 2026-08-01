using Microsoft.EntityFrameworkCore;
using NutritionApi.Application.Interfaces;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence;

/// <summary>Contexte EF Core de l'application — expose les entités du domaine et implémente l'unité de travail.</summary>
public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<DietPlan> DietPlans => Set<DietPlan>();
    public DbSet<Diet> Diets => Set<Diet>();
    public DbSet<Meal> Meals => Set<Meal>();
    public DbSet<MealItem> MealItems => Set<MealItem>();
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<FoodItem> FoodItems => Set<FoodItem>();
    public DbSet<SavedFoodItem> SavedFoodItems => Set<SavedFoodItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    /// <summary>Persiste toutes les modifications en attente dans la base de données.</summary>
    async Task IUnitOfWork.SaveChangesAsync() => await SaveChangesAsync();
}
