using NutritionApi.Application.DTOS.DietPlans;
using NutritionApi.Application.DTOS.Diets;
using NutritionApi.Application.DTOS.FoodItems;
using NutritionApi.Application.DTOS.Meals;
using NutritionApi.Application.DTOS.Users;

/// <summary>Export complet des données personnelles de l'utilisateur (RGPD Art. 20).</summary>
/// <param name="Profile">Profil de l'utilisateur.</param>
/// <param name="WeightHistory">Historique complet des pesées.</param>
/// <param name="DietPlans">Plans diététiques personnels.</param>
/// <param name="Diets">Régimes actifs et archivés.</param>
/// <param name="Meals">Repas enregistrés.</param>
/// <param name="SavedFoodItems">Aliments favoris.</param>
public record UserExportResponse(
    UserProfileResponse Profile,
    List<WeightEntryResponse> WeightHistory,
    List<DietPlanResponse> DietPlans,
    List<DietResponse> Diets,
    List<MealResponse> Meals,
    List<FoodItemSearchResponse> SavedFoodItems
);
