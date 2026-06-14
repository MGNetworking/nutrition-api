using NutritionApi.Domain.Enums;

namespace NutritionApi.Application.Services.Nutrition;

public static class NutritionCalculatorFactory
{
    public static NutritionCalculator Create(BmrFormula formula = BmrFormula.MifflinStJeor)
    {
        IBmrStrategy strategy = formula switch
        {
            BmrFormula.MifflinStJeor => new MifflinStJeorStrategy(),
            BmrFormula.HarrisBenedict => new HarrisBenedictStrategy(),
            _ => throw new ArgumentException($"Unknown BMR formula. Received: {formula}", nameof(formula))
        };

        return new NutritionCalculator(strategy);
    }
}
