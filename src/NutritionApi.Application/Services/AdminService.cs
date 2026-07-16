namespace NutritionApi.Application.Services;

using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

/// <summary>
/// Implémentation de <see cref="IAdminService"/>.
/// Fournit les indicateurs d'administration de la plateforme : KPIs utilisateurs, santé système et gestion des templates.
/// </summary>
public class AdminService : IAdminService
{
    private readonly IUserRepository _userRepository;
    private readonly IDietRepository _dietRepository;
    private readonly IMealRepository _mealRepository;
    private readonly IFoodItemRepository _foodItemRepository;
    private readonly IDietPlanRepository _dietPlanRepository;
    private readonly IJobMonitoringService _jobMonitoringService;

    public AdminService(
        IUserRepository userRepository,
        IDietRepository dietRepository,
        IMealRepository mealRepository,
        IFoodItemRepository foodItemRepository,
        IDietPlanRepository dietPlanRepository,
        IJobMonitoringService jobMonitoringService)
    {
        _userRepository = userRepository;
        _dietRepository = dietRepository;
        _mealRepository = mealRepository;
        _foodItemRepository = foodItemRepository;
        _dietPlanRepository = dietPlanRepository;
        _jobMonitoringService = jobMonitoringService;
    }

    /// <summary>Agrège les KPIs utilisateurs de la plateforme : répartition par tier, acquisition et activité sur 7 jours glissants, comptes en grace period.</summary>
    /// <returns>Les indicateurs consolidés du tableau de bord administrateur.</returns>
    public async Task<AdminDashboardResponse> GetDashboardAsync()
    {
        var since = DateTime.UtcNow.AddDays(-7);

        var freeUsers = await _userRepository.CountByTierAsync(SubscriptionTier.Free);
        var proUsers = await _userRepository.CountByTierAsync(SubscriptionTier.Pro);
        var businessUsers = await _userRepository.CountByTierAsync(SubscriptionTier.Business);
        var newUsersLast7Days = await _userRepository.CountCreatedSinceAsync(since);
        var usersInGracePeriod = await _userRepository.CountInGracePeriodAsync();
        var activeDiets = await _dietRepository.CountActiveAsync();
        var mealsLast7Days = await _mealRepository.CountCreatedSinceAsync(since);

        return new AdminDashboardResponse(
            TotalUsers: freeUsers + proUsers + businessUsers,
            UsersByTier: new UsersByTierResponse(freeUsers, proUsers, businessUsers),
            NewUsersLast7Days: newUsersLast7Days,
            ActiveDiets: activeDiets,
            MealsLast7Days: mealsLast7Days,
            UsersInGracePeriod: usersInGracePeriod);
    }

    /// <summary>Agrège l'état de santé du système : statut des jobs planifiés (import Open Food Facts, purge RGPD) et taille du catalogue d'aliments.</summary>
    /// <returns>Le statut système courant — <c>LastImportAt</c> est <c>null</c> si le job d'import n'a jamais été exécuté.</returns>
    public async Task<SystemHealthResponse> GetSystemHealthAsync()
    {
        var jobs = await _jobMonitoringService.GetJobsStatusAsync();
        var foodItemsCount = await _foodItemRepository.CountAsync();

        var lastImportAt = jobs
            .FirstOrDefault(j => j.JobName == IJobMonitoringService.ImportOffJobName)
            ?.LastRun;

        return new SystemHealthResponse(
            FoodItemsCount: foodItemsCount,
            LastImportAt: lastImportAt,
            HangfireJobs: jobs);
    }

    /// <summary>Crée un DietPlan template partagé — <c>IsTemplate</c> forcé à <c>true</c>, sans propriétaire (<c>UserId</c> null).</summary>
    /// <param name="request">Données du template à créer.</param>
    /// <returns>Le template créé.</returns>
    /// <remarks>Le rôle <c>admin</c> est vérifié en amont par le controller (Keycloak).</remarks>
    public async Task<DietPlanResponse> CreateTemplateAsync(CreateDietPlanRequest request)
    {
        var macro = new MacroDistribution(
            request.MacroDistribution.ProteinPct,
            request.MacroDistribution.CarbPct,
            request.MacroDistribution.FatPct);

        var template = new DietPlan(
            userId: null,
            name: request.Name,
            isTemplate: true,
            dietType: request.DietType,
            goal: request.Goal,
            targetWeight: request.TargetWeight ?? 0f,
            macroDistribution: macro);

        await _dietPlanRepository.AddAsync(template);
        return DietPlanResponse.From(template);
    }

    /// <summary>Met à jour un DietPlan template existant.</summary>
    /// <param name="templateId">Identifiant du template à modifier.</param>
    /// <param name="request">Données mises à jour du template.</param>
    /// <returns>Le template mis à jour.</returns>
    /// <exception cref="NotFoundException">Aucun template ne correspond à cet identifiant — y compris si le plan existe mais est un plan personnel.</exception>
    public async Task<DietPlanResponse> UpdateTemplateAsync(Guid templateId, UpdateDietPlanRequest request)
    {
        var template = await GetTemplateOrThrowAsync(templateId);

        var macro = new MacroDistribution(
            request.MacroDistribution.ProteinPct,
            request.MacroDistribution.CarbPct,
            request.MacroDistribution.FatPct);

        template.Rename(request.Name);
        template.ChangeDietType(request.DietType);
        template.ChangeGoal(request.Goal);
        if (request.TargetWeight.HasValue)
            template.SetTargetWeight(request.TargetWeight.Value);
        template.AdjustMacros(macro);

        await _dietPlanRepository.UpdateAsync(template);
        return DietPlanResponse.From(template);
    }

    /// <summary>Supprime un DietPlan template.</summary>
    /// <param name="templateId">Identifiant du template à supprimer.</param>
    /// <exception cref="NotFoundException">Aucun template ne correspond à cet identifiant — y compris si le plan existe mais est un plan personnel.</exception>
    public async Task DeleteTemplateAsync(Guid templateId)
    {
        await GetTemplateOrThrowAsync(templateId);
        await _dietPlanRepository.DeleteAsync(templateId);
    }

    /// <summary>Retourne le template demandé — un plan personnel n'est jamais atteignable par les opérations admin de templates.</summary>
    /// <exception cref="NotFoundException">Le plan n'existe pas ou n'est pas un template.</exception>
    private async Task<DietPlan> GetTemplateOrThrowAsync(Guid templateId)
    {
        var plan = await _dietPlanRepository.GetByIdAsync(templateId);
        if (plan is null || !plan.IsTemplate)
            throw new NotFoundException("Template not found.");
        return plan;
    }
}
