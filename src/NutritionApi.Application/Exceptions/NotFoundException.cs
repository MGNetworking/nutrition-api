namespace NutritionApi.Application.Exceptions;

/// <summary>Levée quand la ressource demandée n'existe pas — traduite en 404 Not Found par le middleware d'exceptions.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
