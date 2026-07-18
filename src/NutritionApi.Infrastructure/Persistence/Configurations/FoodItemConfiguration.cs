using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="FoodItem"/> — table <c>food_items</c>.</summary>
public class FoodItemConfiguration : IEntityTypeConfiguration<FoodItem>
{
    public void Configure(EntityTypeBuilder<FoodItem> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OffId)
               .IsRequired()
               .HasMaxLength(100);
        builder.HasIndex(f => f.OffId).IsUnique();

        builder.Property(f => f.Name)
               .IsRequired()
               .HasMaxLength(500);
        builder.HasIndex(f => f.Name);

        builder.Property(f => f.AllergensTags)
               .HasColumnType("text[]")
               .HasConversion(
                   v => v.Select(a => a.ToString()).ToArray(),
                   v => v.Select(s => Enum.Parse<Allergen>(s)).ToList(),
                   new ValueComparer<List<Allergen>>(
                       (a, b) => a!.SequenceEqual(b!),
                       v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                       v => v.ToList()))
               .HasDefaultValueSql("'{}'");
    }
}
