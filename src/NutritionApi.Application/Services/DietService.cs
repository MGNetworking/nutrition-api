using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.Nutrition;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Application.Services;

/// <summary>
/// Implémentation de <see cref="IDietService"/>.
/// Gère le cycle de vie des Diets : lancement, archivage et consultation.
/// </summary>
public class DietService : IDietService
{
    private readonly IDietRepository _dietRepository;
    private readonly IDietPlanRepository _dietPlanRepository;
    private readonly IUserRepository _userRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly SubscriptionGuard _subscriptionGuard;

    public DietService(
        IDietRepository dietRepository,
        IDietPlanRepository dietPlanRepository,
        IUserRepository userRepository,
        IWeightEntryRepository weightEntryRepository,
        SubscriptionGuard subscriptionGuard)
    {
        _dietRepository = dietRepository;
        _dietPlanRepository = dietPlanRepository;
        _userRepository = userRepository;
        _weightEntryRepository = weightEntryRepository;
        _subscriptionGuard = subscriptionGuard;
    }

    /// <summary>
    /// Lance un DietPlan et crée une Diet active avec snapshot des données nutritionnelles
    /// gelées à la date du lancement. Calcule BMR/TDEE/CalorieTarget depuis le profil
    /// utilisateur et la dernière pesée enregistrée.
    /// Vérifie l'accès aux templates selon le tier si le plan est un template.
    /// </summary>
    /// <exception cref="NotFoundException">Le DietPlan ou l'utilisateur n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le DietPlan n'appartient pas à l'utilisateur.</exception>
    /// <exception cref="ConflictException">Une Diet active existe déjà pour cet utilisateur.</exception>
    /// <exception cref="UnprocessableException">Aucune pesée enregistrée — requise pour le calcul.</exception>
    public async Task<DietResponse> LaunchAsync(Guid userId, Guid planId)
    {
        var plan = await _dietPlanRepository.GetByIdAsync(planId);
        if (plan is null)
            throw new NotFoundException("DietPlan not found.");

        if (!plan.IsTemplate && plan.UserId != userId)
            throw new ForbiddenException("You do not have access to this DietPlan.");

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User not found.");

        if (plan.IsTemplate)
            _subscriptionGuard.CheckTemplateAccess(user.SubscriptionTier);

        var activeDiet = await _dietRepository.GetActiveByUserIdAsync(userId);
        if (activeDiet is not null)
            throw new ConflictException("A diet is already active. End it before launching a new one.");

        var entries = await _weightEntryRepository.GetByUserIdAsync(userId);
        var latestEntry = entries.OrderByDescending(e => e.MeasuredAt).FirstOrDefault();
        if (latestEntry is null)
            throw new UnprocessableException("No weight entry found. A weight entry is required to launch a diet.");

        var calculator = NutritionCalculatorFactory.Create();
        var needs = calculator.Calculate(user, latestEntry.Weight, plan.Goal, plan.MacroDistribution);

        var diet = new Diet(
            userId,
            plan.Name,
            plan.DietType,
            plan.Goal,
            plan.TargetWeight,
            (int)Math.Round(needs.TargetCalories),
            plan.MacroDistribution);

        await _dietRepository.AddAsync(diet);
        return DietResponse.From(diet);
    }

    /// <summary>
    /// Retourne la Diet active de l'utilisateur.
    /// </summary>
    /// <exception cref="NotFoundException">Aucune Diet active pour cet utilisateur.</exception>
    public Task<DietResponse> GetActiveAsync(Guid userId) => throw new NotImplementedException();

    /// <summary>
    /// Retourne l'historique des Diets de l'utilisateur, trié par date de début décroissante.
    /// </summary>
    public Task<List<DietResponse>> GetHistoryAsync(Guid userId) => throw new NotImplementedException();

    /// <summary>
    /// Retourne le détail d'une Diet appartenant à l'utilisateur.
    /// </summary>
    /// <exception cref="NotFoundException">La Diet n'existe pas.</exception>
    /// <exception cref="ForbiddenException">La Diet n'appartient pas à l'utilisateur.</exception>
    public Task<DietResponse> GetByIdAsync(Guid userId, Guid dietId) => throw new NotImplementedException();

    /// <summary>
    /// Archive la Diet active de l'utilisateur et retourne la Diet mise à jour.
    /// </summary>
    /// <exception cref="NotFoundException">La Diet n'existe pas.</exception>
    /// <exception cref="UnprocessableException">La Diet n'est pas en statut actif.</exception>
    public Task<DietResponse> ArchiveAsync(Guid userId, Guid dietId) => throw new NotImplementedException();

    /// <summary>
    /// Retourne le bilan nutritionnel d'une Diet sur une période donnée.
    /// </summary>
    /// <exception cref="NotFoundException">La Diet n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Accès non autorisé selon le tier.</exception>
    public Task<NutritionBilanResponse> GetBilanAsync(Guid userId, Guid dietId, string period, DateOnly? date, DateOnly? startDate, DateOnly? endDate) => throw new NotImplementedException();
}
