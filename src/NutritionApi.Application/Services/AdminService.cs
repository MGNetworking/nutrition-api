namespace NutritionApi.Application.Services;

using NutritionApi.Application.DTOS.Admin;
using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.Interfaces.ExternalServices;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;
using NutritionApi.Domain.Enums;

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
    private readonly IJobMonitoringService _jobMonitoringService;

    public AdminService(
        IUserRepository userRepository,
        IDietRepository dietRepository,
        IMealRepository mealRepository,
        IFoodItemRepository foodItemRepository,
        IJobMonitoringService jobMonitoringService)
    {
        _userRepository = userRepository;
        _dietRepository = dietRepository;
        _mealRepository = mealRepository;
        _foodItemRepository = foodItemRepository;
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

    /// <summary>Non implémenté — prévu par un ticket ultérieur (gestion des templates).</summary>
    public Task<DietPlanResponse> CreateTemplateAsync(CreateDietPlanRequest request)
        => throw new NotImplementedException("Gestion des templates — ticket ultérieur.");

    /// <summary>Non implémenté — prévu par un ticket ultérieur (gestion des templates).</summary>
    public Task<DietPlanResponse> UpdateTemplateAsync(Guid templateId, UpdateDietPlanRequest request)
        => throw new NotImplementedException("Gestion des templates — ticket ultérieur.");

    /// <summary>Non implémenté — prévu par un ticket ultérieur (gestion des templates).</summary>
    public Task DeleteTemplateAsync(Guid templateId)
        => throw new NotImplementedException("Gestion des templates — ticket ultérieur.");
}
