namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Nutrition;
using NutritionApi.Application.Enums;

/// <summary>Contrat applicatif pour le calcul du bilan nutritionnel d'un régime.</summary>
public interface INutritionService
{
    /// <summary>Retourne le bilan nutritionnel d'un régime sur une période donnée.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="dietId">Identifiant du régime.</param>
    /// <param name="period">Découpage temporel demandé ; <c>null</c> pour la durée complète de la Diet.</param>
    /// <param name="date">Date de référence pour Day/Week/Month.</param>
    /// <param name="startDate">Début de la période pour Custom.</param>
    /// <param name="endDate">Fin de la période pour Custom.</param>
    /// <returns>Le bilan nutritionnel agrégé sur la période.</returns>
    Task<NutritionBilanResponse> GetBilanAsync(Guid userId, Guid dietId, BilanPeriod? period, DateOnly? date, DateOnly? startDate, DateOnly? endDate);
}
