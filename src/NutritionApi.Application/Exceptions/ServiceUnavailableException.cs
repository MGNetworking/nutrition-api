namespace NutritionApi.Application.Exceptions;

/// <summary>
/// Levée quand une dépendance externe est injoignable — traduite en 503 Service Unavailable par le
/// middleware d'exceptions.
/// </summary>
/// <remarks>
/// C'est l'abstraction qui évite à la couche API de connaître Npgsql, StackExchange.Redis ou les
/// exceptions de <c>HttpClient</c> : Infrastructure attrape ses échecs techniques et lève celle-ci.
/// <para>
/// À réserver aux dépendances <b>sans repli</b>. Le cache Redis n'en fait pas partie : son
/// indisponibilité est absorbée par un repli sur PostgreSQL, journalisé mais invisible du client.
/// </para>
/// </remarks>
public class ServiceUnavailableException : Exception
{
    /// <summary>Crée l'exception en nommant la dépendance en cause.</summary>
    /// <param name="dependency">Nom de la dépendance injoignable — journalisé, jamais renvoyé au client.</param>
    public ServiceUnavailableException(string dependency) : base($"Dependency unavailable: {dependency}.")
        => Dependency = dependency;

    /// <summary>Crée l'exception en conservant la cause technique d'origine.</summary>
    /// <param name="dependency">Nom de la dépendance injoignable.</param>
    /// <param name="innerException">Exception technique à l'origine de l'échec.</param>
    public ServiceUnavailableException(string dependency, Exception innerException)
        : base($"Dependency unavailable: {dependency}.", innerException)
        => Dependency = dependency;

    /// <summary>Dépendance en cause — destinée au journal, pas à la réponse HTTP.</summary>
    public string Dependency { get; }
}
