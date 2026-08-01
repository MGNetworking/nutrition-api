namespace NutritionApi.Application.Exceptions;

/// <summary>Levée quand l'utilisateur n'a pas le droit d'effectuer l'opération (ownership, limite de tier) — traduite en 403 Forbidden par le middleware d'exceptions.</summary>
public class ForbiddenException(string message) : Exception(message);
