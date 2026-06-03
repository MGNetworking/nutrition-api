namespace NutritionApi.Api.Extensions
{
    public static class UserContextExtensions
    {
        public static Guid GetUserId(this HttpContext context) 
            => (Guid)context.Items["UserId"]!;
    }
}
