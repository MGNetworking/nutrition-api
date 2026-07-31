// Les tests de niveau 3 partagent trois services réels et une base unique. IT-EXT-14 va jusqu'à
// arrêter le conteneur PostgreSQL : deux collections exécutées en parallèle se verraient couper la
// base sous les pieds. La parallélisation est donc désactivée pour tout l'assembly.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// Regroupe les classes de tests de niveau 3 partageant <see cref="IntegrationFactory"/> — donc la
/// même base éphémère et le même démarrage de l'API.
/// </summary>
/// <remarks>
/// Même raisonnement qu'au niveau 2 : <c>ICollectionFixture</c> plutôt que <c>IClassFixture</c>,
/// pour ne payer le démarrage de l'hôte qu'une fois. Le coût est ici plus lourd encore — création
/// de la base, migrations EF Core, initialisation du schéma Hangfire.
/// <para>
/// Conséquence : les classes de la collection s'exécutent séquentiellement et partagent l'état de
/// la base. Chaque test sème les données dont il a besoin et n'attend rien de ce qu'un autre a
/// laissé.
/// </para>
/// </remarks>
[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFactory>
{
    /// <summary>Nom de la collection, à porter par chaque classe de tests de niveau 3.</summary>
    public const string Name = "Intégration externe niveau 3";
}
