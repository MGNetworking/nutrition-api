namespace NutritionApi.Infrastructure.Jobs;

using Hangfire.Dashboard;

/// <summary>
/// Restreint l'accès au dashboard Hangfire (<c>/hangfire</c>) aux utilisateurs
/// authentifiés portant le rôle <c>admin</c>.
/// </summary>
public sealed class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>Autorise l'accès si la requête est authentifiée et porte le rôle <c>admin</c>.</summary>
    /// <param name="context">Contexte du dashboard fourni par Hangfire.</param>
    /// <returns><c>true</c> si l'accès est autorisé, sinon <c>false</c>.</returns>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("admin");
    }
}
