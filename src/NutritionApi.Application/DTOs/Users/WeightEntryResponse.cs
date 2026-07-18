using NutritionApi.Domain.Entity;
using System.Diagnostics.CodeAnalysis;

namespace NutritionApi.Application.DTOS.Users;

/// <summary>Pesée enregistrée de l'utilisateur.</summary>
/// <param name="Id">Identifiant de la pesée.</param>
/// <param name="Weight">Poids mesuré, en kilogrammes.</param>
/// <param name="MeasuredAt">Date de la pesée.</param>
public record WeightEntryResponse(Guid Id, float Weight, DateOnly MeasuredAt)
{
    /// <summary>Construit la réponse à partir de l'entité <see cref="WeightEntry"/>.</summary>
    public static WeightEntryResponse From(WeightEntry weight)
        => new(weight.Id, weight.Weight, weight.MeasuredAt);
}
