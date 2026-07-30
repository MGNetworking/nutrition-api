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

        // Une seule pesée par utilisateur et par date. Cette règle porte sur l'ensemble des lignes :
        // l'entité, qui ne connaît qu'elle-même, ne peut pas la vérifier. Le contrôle applicatif de
        // UserService en donne un 409 lisible dans le cas courant ; cette contrainte est ce qui le
        // garantit réellement — insertions concurrentes et appels sans date fournie compris.
        builder.HasIndex(w => new { w.UserId, w.MeasuredAt }).IsUnique();
    }
}
