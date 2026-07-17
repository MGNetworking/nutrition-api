namespace NutritionApi.Domain.Entity;

using NutritionApi.Domain.Enums;

/// <summary>
/// Agrégat racine — repas enregistré par l'utilisateur à un instant donné.
/// Contient au moins un MealItem pour être valide et n'a pas de lien direct vers une Diet —
/// la Diet active à la date du repas est déduite par le service via StartDate / EndDate.
/// </summary>
public class Meal
{
    /// <summary>Identifiant interne du repas.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant de l'utilisateur propriétaire.</summary>
    public Guid UserId { get; init; }

    /// <summary>Nom du repas.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Type de repas (petit-déjeuner, déjeuner, dîner, collation).</summary>
    public MealType MealType { get; private set; } = MealType.Unknown;

    /// <summary>Notes libres, optionnelles — null si aucune note.</summary>
    public string? Notes { get; private set; }

    /// <summary>Aliments composant le repas — au moins un élément.</summary>
    public List<MealItem> MealItems { get; private set; } = new List<MealItem>();

    /// <summary>Date et heure de consommation du repas.</summary>
    public DateTime ConsumedAt { get; private set; }

    /// <summary>True = repas sauvegardé dans la liste personnalisée (réutilisable) ; false = repas ponctuel.</summary>
    public bool IsSaved { get; private set; }

    /// <summary>Date de saisie du repas dans le système (UTC) — distincte de <see cref="ConsumedAt"/> qui peut être rétroactive.</summary>
    public DateTime CreatedAt { get; private set; }

    // Pour EF Core uniquement
    private Meal() { }

    /// <summary>Crée un repas avec sa composition — un repas doit contenir au moins un MealItem.</summary>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire.</param>
    /// <param name="name">Nom du repas.</param>
    /// <param name="mealType">Type de repas.</param>
    /// <param name="notes">Notes libres — null si aucune.</param>
    /// <param name="mealItems">Aliments du repas — au moins un élément.</param>
    /// <param name="consumedAt">Date et heure de consommation.</param>
    /// <param name="isSaved">True pour sauvegarder le repas dans la liste personnalisée.</param>
    /// <exception cref="ArgumentException">userId est un Guid vide, consumedAt n'est pas défini, mealItems est null ou vide, name est null ou vide, ou mealType vaut Unknown.</exception>
    public Meal(
        Guid userId,
        string name,
        MealType mealType,
        string? notes,
        List<MealItem> mealItems,
        DateTime consumedAt,
        bool isSaved)
    {
        Id = Guid.NewGuid();
        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Received: {userId}", nameof(userId));


        if (consumedAt == default)
            throw new ArgumentException($"Consumed date must be defined. Received: {consumedAt}", nameof(consumedAt));


        if (mealItems == null || mealItems.Count == 0)
            throw new ArgumentException("Meal must contain at least one MealItem.", nameof(mealItems));


        UserId = userId;
        ConsumedAt = consumedAt;
        MealItems = mealItems;
        IsSaved = isSaved;
        CreatedAt = DateTime.UtcNow;

        Rename(name);
        ChangeNote(notes);
        ChangeMealType(mealType);
    }

    /// <summary>Ajoute un aliment au repas.</summary>
    /// <param name="item">MealItem à ajouter.</param>
    /// <exception cref="ArgumentNullException">item est null.</exception>
    public void AddMealItem(MealItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        MealItems.Add(item);
    }

    /// <summary>Retire un aliment du repas — le repas doit toujours contenir au moins un MealItem.</summary>
    /// <param name="mealItemId">Identifiant du MealItem à retirer.</param>
    /// <exception cref="InvalidOperationException">Le repas ne contient qu'un seul MealItem.</exception>
    /// <exception cref="ArgumentException">Aucun MealItem ne correspond à cet identifiant.</exception>
    public void RemoveMealItem(Guid mealItemId)
    {
        if (MealItems.Count <= 1)
            throw new InvalidOperationException("Meal must contain at least one MealItem.");
        var item = MealItems.FirstOrDefault(x => x.Id == mealItemId)
            ?? throw new ArgumentException($"MealItem not found. Received: {mealItemId}", nameof(mealItemId));
        MealItems.Remove(item);
    }

    /// <summary>Renomme le repas.</summary>
    /// <param name="name">Nouveau nom du repas.</param>
    /// <exception cref="ArgumentException">name est null ou vide.</exception>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }

    /// <summary>Modifie les notes du repas — null supprime la note.</summary>
    /// <param name="note">Nouvelle note, ou null pour la supprimer.</param>
    public void ChangeNote(string? note)
    {
        this.Notes = note; // null = suppression volontaire de la note
    }

    /// <summary>Modifie le type de repas.</summary>
    /// <param name="mealType">Nouveau type de repas.</param>
    /// <exception cref="ArgumentException">mealType vaut Unknown.</exception>
    public void ChangeMealType(MealType mealType)
    {
        if (mealType == MealType.Unknown)
            throw new ArgumentException($"Meal type must be defined. Received: {mealType}", nameof(mealType));
        MealType = mealType;
    }

    /// <summary>Modifie la date et l'heure de consommation.</summary>
    /// <param name="consumedAt">Nouvelle date de consommation.</param>
    /// <exception cref="ArgumentException">consumedAt n'est pas défini (valeur par défaut).</exception>
    public void ChangeConsumedAt(DateTime consumedAt)
    {
        if (consumedAt == default)
            throw new ArgumentException($"Consumed date must be defined. Received: {consumedAt}", nameof(consumedAt));
        ConsumedAt = consumedAt;
    }

}
