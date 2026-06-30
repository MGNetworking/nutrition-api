namespace NutritionApi.Application.Interfaces.Services;

using NutritionApi.Application.DTOS.Users;

public interface IUserService
{
    Task<UserProfileResponse> CreateUserProfileAsync(string keycloakId, CreateUserProfileRequest request);
    Task<UserProfileResponse> GetUserProfileAsync(string keycloakId);
    Task<UserProfileResponse> UpdateUserProfileAsync(string keycloakId, UpdateUserProfileRequest request);
    Task<WeightEntryResponse> AddWeightEntryAsync(Guid userId, AddWeightEntryRequest request);
    Task<List<WeightEntryResponse>> GetWeightHistoryAsync(Guid userId);
    Task<WeightEntryResponse> UpdateWeightEntryAsync(Guid userId, Guid entryId, UpdateWeightEntryRequest request);
}
