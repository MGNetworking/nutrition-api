namespace NutritionApi.Application.Services;

using NutritionApi.Application.DTOS.Users;
using NutritionApi.Application.Exceptions;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Application.Interfaces.Services;

public class RgpdService : IRgpdService
{
    private readonly IUserRepository _userRepository;
    private readonly IWeightEntryRepository _weightEntryRepository;
    private readonly IDietPlanRepository _dietPlanRepository;
    private readonly IDietRepository _dietRepository;
    private readonly IMealRepository _mealRepository;
    private readonly ISavedFoodItemRepository _savedFoodItemRepository;

    public RgpdService(
        IUserRepository userRepository,
        IWeightEntryRepository weightEntryRepository,
        IDietPlanRepository dietPlanRepository,
        IDietRepository dietRepository,
        IMealRepository mealRepository,
        ISavedFoodItemRepository savedFoodItemRepository)
    {
        _userRepository = userRepository;
        _weightEntryRepository = weightEntryRepository;
        _dietPlanRepository = dietPlanRepository;
        _dietRepository = dietRepository;
        _mealRepository = mealRepository;
        _savedFoodItemRepository = savedFoodItemRepository;
    }

    public async Task<UserExportResponse> ExportUserDataAsync(string keycloakId)
        => throw new NotImplementedException();
}
