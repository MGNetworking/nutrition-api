namespace NutritionApi.Application.DTOS.Admin;

public record HangfireJobResponse(string JobName, DateTime? LastRun, DateTime? NextRun, string Status);

public record SystemHealthResponse(
    int FoodItemsCount,
    DateTime? LastImportAt,
    List<HangfireJobResponse> HangfireJobs
);
