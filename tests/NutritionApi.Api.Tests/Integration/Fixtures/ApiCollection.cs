namespace NutritionApi.Api.Tests.Integration.Fixtures;

/// <summary>
/// Regroupe toutes les classes de tests de niveau 2 dans une même collection xUnit, afin que
/// <see cref="ApiFactory"/> — et donc le démarrage de l'API — soit partagé par l'ensemble.
/// </summary>
/// <remarks>
/// Avec <c>IClassFixture</c>, l'API redémarrait une fois par classe de test : le coût de démarrage
/// se multipliait par le nombre de fichiers. <c>ICollectionFixture</c> le ramène à une seule fois
/// pour toute la suite.
/// <para>
/// Conséquence à connaître : xUnit exécute les classes d'une même collection **séquentiellement**,
/// et les doublures de la fabrique sont partagées. Chaque test doit donc armer explicitement les
/// doublures qu'il utilise, sans jamais compter sur l'état laissé par un autre.
/// </para>
/// </remarks>
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    /// <summary>Nom de la collection, à porter par chaque classe de tests de niveau 2.</summary>
    public const string Name = "API niveau 2";
}
