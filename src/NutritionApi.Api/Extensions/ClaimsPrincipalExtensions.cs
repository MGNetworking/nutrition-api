namespace NutritionApi.Api.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this HttpContext context) 
            => (Guid)context.Items["UserId"]!;
            
    }
}
