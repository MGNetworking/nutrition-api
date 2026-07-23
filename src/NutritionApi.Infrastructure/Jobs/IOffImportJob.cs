namespace NutritionApi.Infrastructure.Jobs;

/// <summary>Job d'import du dump Open Food Facts vers le catalogue local d'aliments.</summary>
public interface IOffImportJob
{
    /// <summary>Télécharge le dump Open Food Facts et met le catalogue <c>FoodItem</c> à jour, par lots.</summary>
    Task RunAsync();
}
