using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="Meal"/> — table <c>meals</c>.</summary>
public class MealConfiguration : IEntityTypeConfiguration<Meal>
{
    public void Configure(EntityTypeBuilder<Meal> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(m => m.UserId);

        builder.Property(m => m.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(m => m.MealType)
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(m => m.IsSaved).HasDefaultValue(false);

        // La suppression d'un repas emporte ses items
        builder.HasMany(m => m.MealItems)
               .WithOne()
               .HasForeignKey(mi => mi.MealId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
