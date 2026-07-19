using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

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

            macro.HasData(
                new { DietPlanId = TemplatePerteDePoidsId, ProteinPercentage = 35, CarbPercentage = 35, FatPercentage = 30 },
                new { DietPlanId = TemplateMaintienId, ProteinPercentage = 25, CarbPercentage = 45, FatPercentage = 30 },
                new { DietPlanId = TemplatePriseDeMasseId, ProteinPercentage = 30, CarbPercentage = 50, FatPercentage = 20 },
                new { DietPlanId = TemplateEquilibreId, ProteinPercentage = 20, CarbPercentage = 50, FatPercentage = 30 });
        });

        // Seed des templates partagés (IsTemplate = true, UserId = null) — Guids fixes exigés par HasData
        builder.HasData(
            new { Id = TemplatePerteDePoidsId, UserId = (Guid?)null, Name = "Perte de poids", IsTemplate = true, DietType = DietType.Balanced, Goal = Goal.WeightLoss, TargetWeight = 0f },
            new { Id = TemplateMaintienId, UserId = (Guid?)null, Name = "Maintien", IsTemplate = true, DietType = DietType.Balanced, Goal = Goal.Maintenance, TargetWeight = 0f },
            new { Id = TemplatePriseDeMasseId, UserId = (Guid?)null, Name = "Prise de masse", IsTemplate = true, DietType = DietType.HighProtein, Goal = Goal.WeightGain, TargetWeight = 0f },
            new { Id = TemplateEquilibreId, UserId = (Guid?)null, Name = "Équilibre méditerranéen", IsTemplate = true, DietType = DietType.Mediterranean, Goal = Goal.Maintenance, TargetWeight = 0f });
    }

    private static readonly Guid TemplatePerteDePoidsId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid TemplateMaintienId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid TemplatePriseDeMasseId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    private static readonly Guid TemplateEquilibreId = Guid.Parse("11111111-1111-1111-1111-111111111104");
}
