namespace NutritionApi.Application.Services;

using NutritionApi.Application.DTOS.Nutrition;
using NutritionApi.Application.Enums;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;

/// <summary>
/// Implémentation de <see cref="INutritionService"/>.
/// Calcule le bilan nutritionnel agrégé d'une Diet à partir des repas et des pesées de l'utilisateur.
/// </summary>
public class NutritionService : INutritionService
{
    private readonly IDietRepository _dietRepository;
    private readonly IUserRepository _userRepository;
    private readonly SubscriptionGuard _subscriptionGuard;
    private readonly IMealRepository _mealRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;

    public NutritionService(
        IDietRepository dietRepository,
        SubscriptionGuard subscriptionGuard,
        IUserRepository userRepository,
        IMealRepository mealRepository,
        IWeightEntryRepository weightEntryRepository
        )
    {
        _userRepository = userRepository;
        _dietRepository = dietRepository;
        _subscriptionGuard = subscriptionGuard;
        _mealRepository = mealRepository;
        _weightEntryRepository = weightEntryRepository;
    }

    /// <summary>Agrège les repas et les pesées d'une Diet sur une période pour produire son bilan nutritionnel.</summary>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire de la Diet.</param>
    /// <param name="dietId">Identifiant de la Diet concernée.</param>
    /// <param name="period">Découpage temporel demandé ; <c>null</c> pour la durée complète de la Diet.</param>
    /// <param name="date">Date de référence pour Day/Week/Month.</param>
    /// <param name="startDate">Début de la période pour Custom.</param>
    /// <param name="endDate">Fin de la période pour Custom.</param>
    /// <returns>Le bilan nutritionnel agrégé sur la période effective (intersection de la période demandée et de la fenêtre de la Diet).</returns>
    /// <exception cref="NotFoundException">La Diet ou l'utilisateur n'existe pas.</exception>
    /// <exception cref="ForbiddenException">La Diet n'appartient pas à l'utilisateur.</exception>
    /// <exception cref="ForbiddenException">La période demandée dépasse la limite d'historique du palier d'abonnement.</exception>
    /// <exception cref="UnprocessableException">Les paramètres requis pour le period demandé sont manquants ou incohérents.</exception>
    public async Task<NutritionBilanResponse> GetBilanAsync(Guid userId, Guid dietId, BilanPeriod? period, DateOnly? date, DateOnly? startDate, DateOnly? endDate)
    {
        var diet = await _dietRepository.GetByIdAsync(dietId);
        if (diet is null)
            throw new NotFoundException("Diet not found.");

        if (diet.UserId != userId)
            throw new ForbiddenException("This diet does not belong to the user.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User not found.");

        // La fenêtre réelle de la Diet — un repas hors de cette fenêtre n'appartient pas
        // structurellement à la Diet (pas de lien direct Meal -> Diet).
        var dietEndDate = diet.EndDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Point 5 — résout la période demandée ; period absent = Diet complète.
        var (requestedStart, requestedEnd) = period is null
            ? (diet.StartDate, dietEndDate)
            : ResolvePeriod(period.Value, date, startDate, endDate);

        // Point 4 — contrôle tier sur la profondeur d'historique demandée (Free 7j / Pro 1 an)
        _subscriptionGuard.CheckBilanPeriod(user.SubscriptionTier, requestedStart, requestedEnd);

        var effectiveStart = requestedStart > diet.StartDate ? requestedStart : diet.StartDate;
        var effectiveEnd = requestedEnd < dietEndDate ? requestedEnd : dietEndDate;

        // Point 1 — plus de filtre date au repository (sémantique non implémentée/ambiguë) : on récupère
        // tout et on filtre nous-mêmes avec une logique qu'on maîtrise.
        var allMeals = await _mealRepository.GetByUserIdAsync(userId);
        var allWeightEntries = await _weightEntryRepository.GetByUserIdAsync(userId);

        var daily = allMeals
            .Where(meal =>
                DateOnly.FromDateTime(meal.ConsumedAt) >= effectiveStart &&
                DateOnly.FromDateTime(meal.ConsumedAt) <= effectiveEnd)
            // Point 2 — projette la date SANS l'heure avant de grouper, pour fusionner
            // tous les repas d'un même jour calendaire dans un seul groupe.
            .SelectMany(meal => meal.MealItems, (meal, mealItem) => new { Date = DateOnly.FromDateTime(meal.ConsumedAt), Item = mealItem })
            .GroupBy(x => x.Date)
            .Select(grp => new DailyBreakdownEntry(
                Date: grp.Key,
                // Point 3 — utilise le snapshot Nutrition (déjà scalé par Quantity à la création
                // du MealItem), plutôt que FoodItem.CaloriesPer100g (non scalé, navigation nullable).
                Calories: grp.Sum(x => x.Item.Nutrition.Calories),
                Proteins: grp.Sum(x => x.Item.Nutrition.Proteins),
                Carbs: grp.Sum(x => x.Item.Nutrition.Carbs),
                Fats: grp.Sum(x => x.Item.Nutrition.Fats)))
            .OrderBy(d => d.Date)
            .ToList();

        var weight = allWeightEntries
            .Where(w => w.MeasuredAt >= effectiveStart && w.MeasuredAt <= effectiveEnd)
            .Select(w => new WeightProgressionEntry(w.MeasuredAt, w.Weight))
            .OrderBy(d => d.Date)
            .ToList();

        return new NutritionBilanResponse(
            diet.Id,
            StartDate: effectiveStart,
            EndDate: effectiveEnd,
            TotalCalories: daily.Sum(x => x.Calories),
            TotalProteins: daily.Sum(x => x.Proteins),
            TotalCarbs: daily.Sum(x => x.Carbs),
            TotalFats: daily.Sum(x => x.Fats),
            DailyBreakdown: daily,
            WeightProgression: weight
            );
    }

    private static (DateOnly Start, DateOnly End) ResolvePeriod(BilanPeriod period, DateOnly? date, DateOnly? startDate, DateOnly? endDate)
    {
        switch (period)
        {
            case BilanPeriod.Day:
                if (date is null)
                    throw new UnprocessableException("date is required for period 'Day'.");
                return (date.Value, date.Value);

            case BilanPeriod.Week:
                if (date is null)
                    throw new UnprocessableException("date is required for period 'Week'.");
                var mondayOffset = ((int)date.Value.DayOfWeek + 6) % 7;
                var weekStart = date.Value.AddDays(-mondayOffset);
                return (weekStart, weekStart.AddDays(6));

            case BilanPeriod.Month:
                if (date is null)
                    throw new UnprocessableException("date is required for period 'Month'.");
                var monthStart = new DateOnly(date.Value.Year, date.Value.Month, 1);
                return (monthStart, monthStart.AddMonths(1).AddDays(-1));

            case BilanPeriod.Custom:
                if (startDate is null || endDate is null)
                    throw new UnprocessableException("startDate and endDate are required for period 'Custom'.");
                if (startDate > endDate)
                    throw new UnprocessableException("startDate must be before or equal to endDate.");
                return (startDate.Value, endDate.Value);

            default:
                throw new UnprocessableException($"Unknown period. Received: {period}");
        }
    }
}
