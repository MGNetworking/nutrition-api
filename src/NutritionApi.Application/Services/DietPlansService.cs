using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Application.Services.Nutrition;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Application.Services;

public class DietPlansService : IDietPlanService
{
    private readonly IDietPlanRepository _dietPlanRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDietRepository _dietRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly SubscriptionGuard _subscriptionGuard;

    public DietPlansService(
        IDietPlanRepository dietPlanRepository,
        IUserRepository userRepository,
        IDietRepository dietRepository,
        IWeightEntryRepository weightEntryRepository,
        SubscriptionGuard subscriptionGuard)
    {
        _dietPlanRepository = dietPlanRepository;
        _userRepository = userRepository;
        _dietRepository = dietRepository;
        _weightEntryRepository = weightEntryRepository;
        _subscriptionGuard = subscriptionGuard;
    }

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

    public async Task<List<DietPlanResponse>> GetUserPlansAsync(Guid userId)
    {
        var plans = await _dietPlanRepository.GetByUserIdAsync(userId);
        return plans.Select(DietPlanResponse.From).ToList();
    }

    public async Task<List<DietPlanResponse>> GetTemplatesAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new NotFoundException("User not found.");

        _subscriptionGuard.CheckTemplateAccess(user.SubscriptionTier);

        var templates = await _dietPlanRepository.GetTemplatesAsync();
        return templates.Select(DietPlanResponse.From).ToList();
    }

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

    public async Task DeleteAsync(Guid userId, Guid planId)
    {
        var plan = await _dietPlanRepository.GetByIdAsync(planId);
        if (plan is null)
            throw new NotFoundException("DietPlan not found.");

        if (plan.UserId != userId)
            throw new ForbiddenException("You do not have access to this DietPlan.");

        await _dietPlanRepository.DeleteAsync(planId);
    }

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
}
