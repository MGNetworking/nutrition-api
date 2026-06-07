using NutritionApi.Application.DTOS.Users;
using NutritionApi.Domain.Entity;

namespace NutritionApi.Application.DTOs.Users;

public static class UserMappingDto
{
    public static UserProfileResponse UserToUserProfileResponse(User user)
    {
       return new UserProfileResponse(
              user.Id,
              user.BirthDate,
              user.Gender,
              user.ActivityLevel,
              user.Height,
              user.Allergies,
              user.DietaryPreferences,
              user.SubscriptionTier,
              user.CreatedAt);
    }
}
