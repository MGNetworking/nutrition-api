using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="Diet"/> — table <c>diets</c>.</summary>
public class DietConfiguration : IEntityTypeConfiguration<Diet>
{
    public void Configure(EntityTypeBuilder<Diet> builder)
    {
        builder.HasKey(d => d.Id);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(d => d.UserId);

        builder.Property(d => d.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(d => d.DietType)
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.Property(d => d.Goal)
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.OwnsOne(d => d.MacroDistribution, macro =>
        {
            macro.Property(m => m.ProteinPercentage).HasColumnName("macro_protein_pct");
            macro.Property(m => m.CarbPercentage).HasColumnName("macro_carb_pct");
            macro.Property(m => m.FatPercentage).HasColumnName("macro_fat_pct");
        });

        builder.Property(d => d.StatusDiet)
               .HasColumnName("diet_status")
               .HasConversion<string>()
               .HasMaxLength(20)
               .HasDefaultValue(DietStatus.Active);
        builder.HasIndex(d => d.StatusDiet);
    }
}
