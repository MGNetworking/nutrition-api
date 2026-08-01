using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="SavedFoodItem"/> — table <c>saved_food_items</c>.</summary>
public class SavedFoodItemConfiguration : IEntityTypeConfiguration<SavedFoodItem>
{
    public void Configure(EntityTypeBuilder<SavedFoodItem> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(s => s.UserId);

        builder.HasOne<FoodItem>()
               .WithMany()
               .HasForeignKey(s => s.FoodItemId);

        // Un aliment ne peut être sauvegardé qu'une fois par utilisateur
        builder.HasIndex(s => new { s.UserId, s.FoodItemId }).IsUnique();
    }
}
