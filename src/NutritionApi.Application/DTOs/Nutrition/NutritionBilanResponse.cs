
namespace NutritionApi.Application.DTOS.Nutrition;

/// <summary>Totaux nutritionnels d'une journée de la période du bilan.</summary>
/// <param name="Date">Date du jour agrégé.</param>
/// <param name="Calories">Total des calories consommées ce jour-là, en kcal.</param>
/// <param name="Proteins">Total des protéines consommées ce jour-là, en grammes.</param>
/// <param name="Carbs">Total des glucides consommés ce jour-là, en grammes.</param>
/// <param name="Fats">Total des lipides consommés ce jour-là, en grammes.</param>
public record DailyBreakdownEntry(
    DateOnly Date,
    float Calories,
    float Proteins,
    float Carbs,
    float Fats);

/// <summary>Pesée de l'utilisateur enregistrée dans la période du bilan.</summary>
/// <param name="Date">Date de la pesée.</param>
/// <param name="Weight">Poids corporel de l'utilisateur, en kilogrammes.</param>
public record WeightProgressionEntry(
    DateOnly Date,
    float Weight);

/// <summary>Bilan nutritionnel agrégé d'une Diet sur une période.</summary>
/// <param name="DietId">Identifiant de la Diet concernée.</param>
/// <param name="StartDate">Date de début de la période effective du bilan.</param>
/// <param name="EndDate">Date de fin de la période effective du bilan.</param>
/// <param name="TotalCalories">Total des calories consommées sur la période, en kcal.</param>
/// <param name="TotalProteins">Total des protéines consommées sur la période, en grammes.</param>
/// <param name="TotalCarbs">Total des glucides consommés sur la période, en grammes.</param>
/// <param name="TotalFats">Total des lipides consommés sur la période, en grammes.</param>
/// <param name="DailyBreakdown">Détail des totaux nutritionnels, jour par jour, sur la période.</param>
/// <param name="WeightProgression">Pesées de l'utilisateur enregistrées sur la période.</param>
public record NutritionBilanResponse(
    Guid DietId,
    DateOnly StartDate,
    DateOnly EndDate,
    float TotalCalories,
    float TotalProteins,
    float TotalCarbs,
    float TotalFats,
    List<DailyBreakdownEntry> DailyBreakdown,
    List<WeightProgressionEntry> WeightProgression
);
