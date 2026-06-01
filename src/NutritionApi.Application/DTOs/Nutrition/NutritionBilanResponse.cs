namespace NutritionApi.Application.DTOS.Nutrition;

public record DailyBreakdownEntry(DateOnly Date, float Calories, float Proteins, float Carbs, float Fats);

public record WeightProgressionEntry(DateOnly Date, float Weight);

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
