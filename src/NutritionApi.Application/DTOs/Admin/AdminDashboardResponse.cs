
namespace NutritionApi.Application.DTOS.Admin;

public record UsersByTierResponse(int Free, int Pro, int Business);

public record AdminDashboardResponse(
    int TotalUsers,
    UsersByTierResponse UsersByTier,
    int NewUsersLast7Days,
    int ActiveDiets,
    int MealsLast7Days,
    int UsersInGracePeriod
);
