namespace NutritionApi.Api.Extensions
{
    /// <summary>Fournit l'accès aux données utilisateur résolues par le middleware.</summary>
    public static class UserContextExtensions
    {
        /// <summary>Retourne l'identifiant de l'utilisateur résolu par <c>UserResolutionMiddleware</c>.</summary>
        /// <param name="context">Contexte HTTP de la requête courante.</param>
        /// <returns>L'identifiant de l'utilisateur.</returns>
        public static Guid GetUserId(this HttpContext context)
            => (Guid)context.Items["UserId"]!;
    }
}
