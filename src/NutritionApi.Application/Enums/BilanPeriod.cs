namespace NutritionApi.Application.Enums;

/// <summary>Découpage temporel demandé pour le calcul d'un bilan nutritionnel.</summary>
public enum BilanPeriod
{
    /// <summary>Un seul jour, désigné par le paramètre <c>date</c>.</summary>
    Day,

    /// <summary>Semaine (lundi à dimanche) contenant le paramètre <c>date</c>.</summary>
    Week,

    /// <summary>Mois calendaire contenant le paramètre <c>date</c>.</summary>
    Month,

    /// <summary>Plage personnalisée, désignée par <c>startDate</c> et <c>endDate</c>.</summary>
    Custom
}
