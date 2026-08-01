using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="MealItem"/> — table <c>meal_items</c>.</summary>
public class MealItemConfiguration : IEntityTypeConfiguration<MealItem>
{
    public void Configure(EntityTypeBuilder<MealItem> builder)
    {
        builder.HasKey(mi => mi.Id);

        builder.HasOne(mi => mi.FoodItem)
               .WithMany()
               .HasForeignKey(mi => mi.FoodItemId);

        builder.OwnsOne(mi => mi.Nutrition, n =>
        {
            n.Property(x => x.Calories).HasColumnName("nutrition_calories");
            n.Property(x => x.Proteins).HasColumnName("nutrition_proteins");
            n.Property(x => x.Carbs).HasColumnName("nutrition_carbs");
            n.Property(x => x.Fats).HasColumnName("nutrition_fats");
        });
    }
}
