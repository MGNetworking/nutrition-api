using NutritionApi.Application.Interfaces.Repositories;
using System.Security.Claims;

namespace NutritionApi.Api.Middleware;

/// <summary>
/// Traduit l'identité Keycloak en identifiant applicatif, et dépose le résultat dans
/// <c>HttpContext.Items["UserId"]</c> à l'usage des controllers.
/// </summary>
/// <remarks>
/// Une requête authentifiée dont le claim <c>sub</c> ne correspond à aucun profil est refusée par un
/// 401 : un jeton valide ne suffit pas, encore faut-il exister dans l'application.
/// <para>
/// Les actions marquées <see cref="AllowWithoutProfileAttribute"/> échappent à cette règle — sans
/// quoi la création de profil serait inatteignable, puisqu'il faudrait déjà en posséder un.
/// </para>
/// </remarks>
public class UserResolutionMiddleware : IMiddleware
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserResolutionMiddleware> _logger;

    public UserResolutionMiddleware(
        IUserRepository userRepository,
        ILogger<UserResolutionMiddleware> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated == true && !AllowsMissingProfile(context))
        {
            var keycloakId = context.User.FindFirstValue("sub");
            var user = await _userRepository.GetByKeycloakIdAsync(keycloakId!);
            if (user is null) { context.Response.StatusCode = 401; return; }
            context.Items["UserId"] = user.Id;

            // Attache l'identifiant applicatif à toute entrée écrite en aval (NTR-137). C'est le
            // seul endroit qui le connaisse : RequestLoggingMiddleware s'exécute avant la
            // résolution, et les controllers n'ont pas à s'en soucier.
            //
            // L'identifiant de trace n'est pas ajouté ici — Serilog l'attache lui-même à chaque
            // entrée, y compris celles des requêtes anonymes que ce middleware ne traite pas.
            using (_logger.BeginScope(new Dictionary<string, object> { ["UserId"] = user.Id }))
            {
                await next(context);
                return;
            }
        }

        await next(context);
    }

    /// <summary>
    /// Indique si l'action visée accepte d'être appelée sans profil existant.
    /// </summary>
    /// <remarks>
    /// Le routage a déjà déterminé l'action — ce middleware s'exécute après <c>UseAuthorization</c> —
    /// mais celle-ci n'est pas encore exécutée : c'est la fenêtre où ses attributs sont consultables.
    /// </remarks>
    /// <param name="context">Contexte de la requête en cours.</param>
    /// <returns><c>true</c> si l'action porte <see cref="AllowWithoutProfileAttribute"/>.</returns>
    private static bool AllowsMissingProfile(HttpContext context)
        => context.GetEndpoint()?.Metadata.GetMetadata<AllowWithoutProfileAttribute>() is not null;
}
