namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Nutrition;

/// <summary>Contrat applicatif pour le calcul du bilan nutritionnel d'un régime.</summary>
public interface INutritionService
{
    /// <summary>Retourne le bilan nutritionnel d'un régime sur une période donnée.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="dietId">Identifiant du régime.</param>
    /// <param name="period">Période d'analyse (ex : week, month, custom).</param>
    /// <param name="date">Date de référence pour les périodes prédéfinies.</param>
    /// <param name="startDate">Début de la période pour une plage personnalisée.</param>
    /// <param name="endDate">Fin de la période pour une plage personnalisée.</param>
    /// <returns>Le bilan nutritionnel agrégé sur la période.</returns>
    Task<NutritionBilanResponse> GetBilanAsync(Guid userId, Guid dietId, string period, DateOnly? date, DateOnly? startDate, DateOnly? endDate);
}
