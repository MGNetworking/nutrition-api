using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Infrastructure.Persistence.Configurations;

/// <summary>Mapping EF Core de l'entité <see cref="WeightEntry"/> — table <c>weight_entries</c>.</summary>
public class WeightEntryConfiguration : IEntityTypeConfiguration<WeightEntry>
{
    public void Configure(EntityTypeBuilder<WeightEntry> builder)
    {
        builder.HasKey(w => w.Id);

        builder.HasOne<User>()
               .WithMany()
               .HasForeignKey(w => w.UserId);

        builder.HasIndex(w => w.MeasuredAt);
    }
}
