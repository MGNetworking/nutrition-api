namespace NutritionApi.Application.Exceptions;

/// <summary>Levée quand la requête entre en conflit avec l'état actuel des données (doublon, ressource déjà existante) — traduite en 409 Conflict par le middleware d'exceptions.</summary>
public class ConflictException(string message) : Exception(message);
