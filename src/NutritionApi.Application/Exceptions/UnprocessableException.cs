namespace NutritionApi.Application.Exceptions;

/// <summary>Levée quand la requête est valide en forme mais viole une règle métier — traduite en 422 Unprocessable Entity par le middleware d'exceptions.</summary>
public class UnprocessableException(string message) : Exception(message);
