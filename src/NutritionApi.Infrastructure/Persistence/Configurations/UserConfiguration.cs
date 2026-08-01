using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="User"/> — table <c>users</c>.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.KeycloakId)
               .IsRequired()
               .HasMaxLength(255);
        builder.HasIndex(u => u.KeycloakId).IsUnique();

        builder.Property(u => u.BirthDate).IsRequired();

        builder.Property(u => u.Gender)
               .HasConversion<string>()
               .HasMaxLength(20);

        builder.Property(u => u.ActivityLevel)
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.Property(u => u.Allergies)
               .HasColumnType("text[]")
               .HasConversion(
                   v => v.Select(a => a.ToString()).ToArray(),
                   v => v.Select(s => Enum.Parse<Allergen>(s)).ToList(),
                   new ValueComparer<List<Allergen>>(
                       (a, b) => a!.SequenceEqual(b!),
                       v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                       v => v.ToList()))
               .HasDefaultValueSql("'{}'");

        builder.Property(u => u.DietaryPreferences)
               .HasColumnType("text[]")
               .HasConversion(
                   v => v.Select(p => p.ToString()).ToArray(),
                   v => v.Select(s => Enum.Parse<DietaryPreference>(s)).ToList(),
                   new ValueComparer<List<DietaryPreference>>(
                       (a, b) => a!.SequenceEqual(b!),
                       v => v.Aggregate(0, (h, e) => HashCode.Combine(h, e.GetHashCode())),
                       v => v.ToList()))
               .HasDefaultValueSql("'{}'");

        builder.Property(u => u.SubscriptionTier)
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(SubscriptionTier.Free);
    }
}
