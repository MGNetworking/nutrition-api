using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Application.Services;

/// <summary>
/// Implémentation de <see cref="IDietPlanService"/>.
/// Gère le CRUD des DietPlans personnels et la consultation des templates partagés.
/// </summary>
public class DietPlansService : IDietPlanService
{
    private readonly IDietPlanRepository _dietPlanRepository;
    private readonly IUserRepository _userRepository;
    private readonly SubscriptionGuard _subscriptionGuard;

    public DietPlansService(
        IDietPlanRepository dietPlanRepository,
        IUserRepository userRepository,
        SubscriptionGuard subscriptionGuard)
    {
        _dietPlanRepository = dietPlanRepository;
        _userRepository = userRepository;
        _subscriptionGuard = subscriptionGuard;
    }

    /// <summary>
    /// Crée un DietPlan personnel pour l'utilisateur.
    /// Contrôle la limite du nombre de plans autorisés selon le tier via SubscriptionGuard.
    /// </summary>
    /// <exception cref="NotFoundException">L'utilisateur n'existe pas.</exception>
    public async Task<DietPlanResponse> CreateAsync(Guid userId, CreateDietPlanRequest request)
    {
        var userCurrentCount = await _dietPlanRepository.CountByUserIdAsync(userId);
        var user = await _userRepository.GetByIdAsync(userId);

        if (user is null)
            throw new NotFoundException("User not found.");

        _subscriptionGuard.CheckDietPlanLimit(user.SubscriptionTier, userCurrentCount);

        var macro = new MacroDistribution(
            request.MacroDistribution.ProteinPct,
            request.MacroDistribution.CarbPct,
            request.MacroDistribution.FatPct);

        var planDiet = new DietPlan(userId,
            request.Name,
            false,
            request.DietType,
            request.Goal,
            request.TargetWeight ?? 0f,
            macro);

        await _dietPlanRepository.AddAsync(planDiet);
        return DietPlanResponse.From(planDiet);
    }

    /// <summary>
    /// Retourne tous les DietPlans personnels de l'utilisateur.
    /// </summary>
    public async Task<List<DietPlanResponse>> GetUserPlansAsync(Guid userId)
    {
        var plans = await _dietPlanRepository.GetByUserIdAsync(userId);
        return plans.Select(DietPlanResponse.From).ToList();
    }

    /// <summary>
    /// Retourne les DietPlans templates partagés accessibles à l'utilisateur.
    /// Vérifie l'accès aux templates selon le tier via SubscriptionGuard.
    /// </summary>
    /// <exception cref="NotFoundException">L'utilisateur n'existe pas.</exception>
    public async Task<List<DietPlanResponse>> GetTemplatesAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User not found.");

        _subscriptionGuard.CheckTemplateAccess(user.SubscriptionTier);

        var templates = await _dietPlanRepository.GetTemplatesAsync();
        return templates.Select(DietPlanResponse.From).ToList();
    }

    /// <summary>
    /// Met à jour les données d'un DietPlan appartenant à l'utilisateur.
    /// </summary>
    /// <exception cref="NotFoundException">Le DietPlan n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le DietPlan n'appartient pas à l'utilisateur.</exception>
    public async Task<DietPlanResponse> UpdateAsync(Guid userId, Guid planId, UpdateDietPlanRequest request)
    {
        var plan = await _dietPlanRepository.GetByIdAsync(planId);
        if (plan is null)
            throw new NotFoundException("DietPlan not found.");

        if (plan.UserId != userId)
            throw new ForbiddenException("You do not have access to this DietPlan.");

        var macro = new MacroDistribution(
            request.MacroDistribution.ProteinPct,
            request.MacroDistribution.CarbPct,
            request.MacroDistribution.FatPct);

        plan.Rename(request.Name);
        plan.ChangeDietType(request.DietType);
        plan.ChangeGoal(request.Goal);
        if (request.TargetWeight.HasValue)
            plan.SetTargetWeight(request.TargetWeight.Value);
        plan.AdjustMacros(macro);

        await _dietPlanRepository.UpdateAsync(plan);
        return DietPlanResponse.From(plan);
    }

    /// <summary>
    /// Supprime un DietPlan appartenant à l'utilisateur.
    /// </summary>
    /// <exception cref="NotFoundException">Le DietPlan n'existe pas.</exception>
    /// <exception cref="ForbiddenException">Le DietPlan n'appartient pas à l'utilisateur.</exception>
    public async Task DeleteAsync(Guid userId, Guid planId)
    {
        var plan = await _dietPlanRepository.GetByIdAsync(planId);
        if (plan is null)
            throw new NotFoundException("DietPlan not found.");

        if (plan.UserId != userId)
            throw new ForbiddenException("You do not have access to this DietPlan.");

        await _dietPlanRepository.DeleteAsync(planId);
    }
}
