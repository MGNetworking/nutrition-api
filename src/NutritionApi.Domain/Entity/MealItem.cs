namespace NutritionApi.Domain.Entity;

using ValueObjects;

/// <summary>
/// Entité enfant de Meal — un aliment dans un repas, ne peut pas exister sans son Meal.
/// Porte la quantité consommée en grammes et un snapshot nutritionnel calculé à la création
/// depuis le FoodItem — l'historique ne change jamais rétroactivement.
/// </summary>
public class MealItem
{
    /// <summary>Identifiant interne de l'élément de repas.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant du Meal parent.</summary>
    public Guid MealId { get; init; }

    /// <summary>Référence vers l'aliment de la table locale FoodItem.</summary>
    public Guid FoodItemId { get; init; }

    /// <summary>Quantité consommée en grammes.</summary>
    public float Quantity { get; private set; }

    /// <summary>Snapshot nutritionnel calculé à la création depuis le FoodItem — immuable.</summary>
    public NutritionInfo Nutrition { get; private set; } = null!;

    /// <summary>Navigation EF Core vers l'aliment — chargée via <c>ThenInclude</c>, null sinon.</summary>
    public FoodItem? FoodItem { get; init; }

    // Pour EF Core uniquement
    private MealItem() { }

    /// <summary>Crée un élément de repas avec son snapshot nutritionnel.</summary>
    /// <param name="mealId">Identifiant du Meal parent.</param>
    /// <param name="foodItemId">Identifiant du FoodItem référencé.</param>
    /// <param name="quantity">Quantité consommée en grammes.</param>
    /// <param name="nutrition">Snapshot nutritionnel calculé pour cette quantité.</param>
    /// <exception cref="ArgumentException">mealId ou foodItemId est un Guid vide.</exception>
    /// <exception cref="ArgumentOutOfRangeException">quantity est négatif ou nul.</exception>
    /// <exception cref="ArgumentNullException">nutrition est null.</exception>
    public MealItem(Guid mealId, Guid foodItemId, float quantity, NutritionInfo nutrition)
    {
        Id = Guid.NewGuid();

        if (mealId == Guid.Empty)
            throw new ArgumentException($"Meal ID cannot be empty. Received: {mealId}", nameof(mealId));
        if (foodItemId == Guid.Empty)
            throw new ArgumentException($"Food item ID cannot be empty. Received: {foodItemId}", nameof(foodItemId));

        MealId = mealId;
        FoodItemId = foodItemId;

        SetQuantity(quantity);
        SetNutrition(nutrition);
    }

    private void SetQuantity(float quantity)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        Quantity = quantity;
    }

    private void SetNutrition(NutritionInfo nutrition)
    {
        ArgumentNullException.ThrowIfNull(nutrition);
        Nutrition = nutrition;
    }

}
