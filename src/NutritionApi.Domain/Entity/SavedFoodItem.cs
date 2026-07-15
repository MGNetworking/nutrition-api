namespace NutritionApi.Domain.Entity;

/// <summary>
/// Entité enfant de User — lien personnel et immuable entre un utilisateur et un aliment
/// qu'il souhaite retrouver rapidement lors de la création de ses repas.
/// Un même FoodItem ne peut être sauvegardé qu'une seule fois par User (garde appliquée en couche Application).
/// </summary>
public class SavedFoodItem
{
    /// <summary>Identifiant interne de la sauvegarde.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant de l'utilisateur propriétaire.</summary>
    public Guid UserId { get; init; }

    /// <summary>Référence vers l'aliment de la table locale FoodItem.</summary>
    public Guid FoodItemId { get; init; }

    /// <summary>Date de sauvegarde (UTC).</summary>
    public DateTime SavedAt { get; init; }

    // Pour EF Core uniquement
    private SavedFoodItem() { }

    /// <summary>Crée un lien immuable entre un utilisateur et un aliment — <see cref="SavedAt"/> est fixé à maintenant (UTC).</summary>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire.</param>
    /// <param name="foodItemId">Identifiant du FoodItem à sauvegarder.</param>
    /// <exception cref="ArgumentException">userId ou foodItemId est un Guid vide.</exception>
    public SavedFoodItem(Guid userId, Guid foodItemId)
    {
        Id = Guid.NewGuid();

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        UserId = userId;

        if (foodItemId == Guid.Empty)
            throw new ArgumentException("foodItemId cannot be empty.", nameof(foodItemId));
        FoodItemId = foodItemId;

        SavedAt = DateTime.UtcNow;
    }
}
