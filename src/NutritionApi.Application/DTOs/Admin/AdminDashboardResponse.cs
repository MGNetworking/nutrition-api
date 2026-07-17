
namespace NutritionApi.Application.DTOS.Admin;

/// <summary>Répartition des utilisateurs par palier d'abonnement.</summary>
/// <param name="Free">Nombre d'utilisateurs au palier Free.</param>
/// <param name="Pro">Nombre d'utilisateurs au palier Pro.</param>
/// <param name="Business">Nombre d'utilisateurs au palier Business.</param>
public record UsersByTierResponse(int Free, int Pro, int Business);

/// <summary>KPIs consolidés du tableau de bord administrateur.</summary>
/// <param name="TotalUsers">Nombre total d'utilisateurs, tous paliers confondus.</param>
/// <param name="UsersByTier">Répartition des utilisateurs par palier d'abonnement.</param>
/// <param name="NewUsersLast7Days">Nombre d'utilisateurs créés sur les 7 derniers jours.</param>
/// <param name="ActiveDiets">Nombre de régimes actifs en cours, tous utilisateurs confondus.</param>
/// <param name="MealsLast7Days">Nombre de repas saisis sur les 7 derniers jours.</param>
/// <param name="UsersInGracePeriod">Nombre de comptes en attente de suppression (grace period de 30 jours).</param>
public record AdminDashboardResponse(
    int TotalUsers,
    UsersByTierResponse UsersByTier,
    int NewUsersLast7Days,
    int ActiveDiets,
    int MealsLast7Days,
    int UsersInGracePeriod
);
