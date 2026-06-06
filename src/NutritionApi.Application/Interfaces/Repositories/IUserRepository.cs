using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByKeycloakIdAsync(string keycloakId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
}
