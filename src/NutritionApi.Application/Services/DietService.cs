using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

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
    private readonly IUnitOfWork _unitOfWork;

    public DietService(
        IDietRepository dietRepository,
        IDietPlanRepository dietPlanRepository,
        IUserRepository userRepository,
        IWeightEntryRepository weightEntryRepository,
        SubscriptionGuard subscriptionGuard,
        IUnitOfWork unitOfWork)
    {
        _dietRepository = dietRepository;
        _dietPlanRepository = dietPlanRepository;
        _userRepository = userRepository;
        _weightEntryRepository = weightEntryRepository;
        _subscriptionGuard = subscriptionGuard;
        _unitOfWork = unitOfWork;
    }

    /// <summary>Lance un DietPlan et crée une Diet active avec snapshot des données nutritionnelles gelées à la date du lancement.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="planId">Identifiant du DietPlan à lancer.</param>
    /// <returns>La Diet active créée.</returns>
    /// <exception cref="NotFoundException">Le DietPlan ou l'utilisateur n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le DietPlan n'appartient pas à l'utilisateur.</exception>
    /// <exception cref="ConflictException">Une Diet active existe déjà pour cet utilisateur.</exception>
    /// <exception cref="UnprocessableException">Aucune pesée enregistrée — requise pour le calcul du CalorieTarget.</exception>
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
        await _unitOfWork.SaveChangesAsync();

        return DietResponse.From(diet);
    }

    /// <summary>Retourne la Diet active de l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>La Diet active correspondante.</returns>
    /// <exception cref="NotFoundException">Aucune Diet active pour cet utilisateur.</exception>
    public async Task<DietResponse> GetActiveAsync(Guid userId)
    {
        var diet = await _dietRepository.GetActiveByUserIdAsync(userId);
        if (diet is null)
            throw new NotFoundException("No active diet found.");
        return DietResponse.From(diet);
    }

    /// <summary>Retourne l'historique des Diets de l'utilisateur, trié par date de début décroissante.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <returns>Liste des Diets triée par date de début décroissante.</returns>
    public async Task<List<DietResponse>> GetHistoryAsync(Guid userId)
    {
        var diets = await _dietRepository.GetByUserIdAsync(userId);
        return diets
            .OrderByDescending(d => d.StartDate)
            .Select(DietResponse.From)
            .ToList();
    }

    /// <summary>Retourne le détail d'une Diet appartenant à l'utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="dietId">Identifiant de la Diet.</param>
    /// <returns>La Diet correspondante.</returns>
    /// <exception cref="NotFoundException">La Diet n'existe pas.</exception>
    /// <exception cref="ForbiddenException">La Diet n'appartient pas à l'utilisateur.</exception>
    public async Task<DietResponse> GetByIdAsync(Guid userId, Guid dietId)
    {
        var diet = await _dietRepository.GetByIdAsync(dietId);
        if (diet is null)
            throw new NotFoundException("Diet not found.");
        if (diet.UserId != userId)
            throw new ForbiddenException("You do not have access to this diet.");
        return DietResponse.From(diet);
    }

    /// <summary>Archive la Diet active de l'utilisateur et retourne la Diet mise à jour.</summary>
    /// <param name="userId">Identifiant de l'utilisateur.</param>
    /// <param name="dietId">Identifiant de la Diet à archiver.</param>
    /// <returns>La Diet archivée.</returns>
    /// <exception cref="NotFoundException">La Diet n'existe pas.</exception>
    /// <exception cref="ForbiddenException">La Diet n'appartient pas à l'utilisateur.</exception>
    /// <exception cref="UnprocessableException">La Diet n'est pas en statut actif.</exception>
    public async Task<DietResponse> ArchiveAsync(Guid userId, Guid dietId)
    {
        var diete = await _dietRepository.GetByIdAsync(dietId);

        if (diete is null)
            throw new NotFoundException("Diet not found.");

        if (diete.UserId != userId)
            throw new ForbiddenException("This user cannot activate this diet");

        if (diete.StatusDiet != DietStatus.Active)
            throw new UnprocessableException("This diet is not active");

        diete.ChangeDietStatus(DietStatus.Archived);
        await _dietRepository.UpdateAsync(diete);
        await _unitOfWork.SaveChangesAsync();

        return DietResponse.From(diete);

    }

}
