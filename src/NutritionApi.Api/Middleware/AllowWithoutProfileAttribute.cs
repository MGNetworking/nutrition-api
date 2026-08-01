namespace NutritionApi.Api.Middleware;

/// <summary>
/// Dispense une action de l'exigence d'un profil applicatif existant, sans lever l'exigence
/// d'authentification portée par <c>[Authorize]</c>.
/// </summary>
/// <remarks>
/// <see cref="UserResolutionMiddleware"/> refuse par défaut toute requête authentifiée dont le
/// claim <c>sub</c> ne correspond à aucun <c>User</c> en base. Cette règle est juste partout, sauf
/// à l'endroit précis où le profil est créé : sans cette dispense, il faudrait déjà posséder un
/// profil pour pouvoir en créer un.
/// <para>
/// L'attribut est porté par l'action elle-même plutôt que par une liste de chemins tenue dans le
/// middleware : la dispense suit l'action si sa route change, là où une comparaison de chemin
/// cesserait silencieusement de s'appliquer.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class AllowWithoutProfileAttribute : Attribute;
