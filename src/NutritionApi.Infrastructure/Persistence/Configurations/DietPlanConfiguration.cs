using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="DietPlan"/> — table <c>diet_plans</c>.</summary>
public class DietPlanConfiguration : IEntityTypeConfiguration<DietPlan>
{
    public void Configure(EntityTypeBuilder<DietPlan> builder)
    {
        builder.HasKey(dp => dp.Id);

        // UserId nullable : un template n'a pas de propriétaire
        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(dp => dp.UserId);

        builder.Property(dp => dp.IsTemplate).HasDefaultValue(false);
        builder.HasIndex(dp => dp.IsTemplate);

        builder.Property(dp => dp.Name)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(dp => dp.DietType)
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.Property(dp => dp.Goal)
               .HasConversion<string>()
               .HasMaxLength(30);

        builder.OwnsOne(dp => dp.MacroDistribution, macro =>
        {
            macro.Property(m => m.ProteinPercentage).HasColumnName("macro_protein_pct");
            macro.Property(m => m.CarbPercentage).HasColumnName("macro_carb_pct");
            macro.Property(m => m.FatPercentage).HasColumnName("macro_fat_pct");
        });
    }
}
