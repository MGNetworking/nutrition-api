namespace NutritionApi.Domain.Entity;

/// <summary>
/// Entité enfant de User — enregistrement du poids à un instant donné, ne peut pas exister sans son User.
/// Permet le suivi de la progression pondérale et fournit le poids de référence
/// pour le calcul BMR/TDEE au lancement d'une Diet (entrée la plus récente).
/// </summary>
public class WeightEntry
{
    /// <summary>Identifiant interne de la pesée.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant de l'utilisateur propriétaire.</summary>
    public Guid UserId { get; init; }

    /// <summary>Poids mesuré en kilogrammes.</summary>
    public float Weight { get; private set; }

    /// <summary>Date réelle de la mesure, fournie par l'utilisateur — peut être antérieure à la date d'enregistrement.</summary>
    public DateOnly MeasuredAt { get; private set; }

    private WeightEntry() { }

    /// <summary>Crée une pesée pour un utilisateur.</summary>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire.</param>
    /// <param name="weight">Poids mesuré en kilogrammes.</param>
    /// <param name="measuredAt">Date réelle de la mesure.</param>
    /// <exception cref="ArgumentException">userId est un Guid vide, ou measuredAt n'est pas défini.</exception>
    /// <exception cref="ArgumentOutOfRangeException">weight est négatif ou nul.</exception>
    public WeightEntry(Guid userId, float weight, DateOnly measuredAt)
    {
        Id = Guid.NewGuid();

        if (userId == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));

        UserId = userId;
        Update(weight, measuredAt);
    }

    /// <summary>Corrige le poids et la date de mesure de la pesée.</summary>
    /// <param name="weight">Poids mesuré en kilogrammes.</param>
    /// <param name="measuredAt">Date réelle de la mesure.</param>
    /// <exception cref="ArgumentOutOfRangeException">weight est négatif ou nul.</exception>
    /// <exception cref="ArgumentException">measuredAt n'est pas défini (valeur par défaut).</exception>
    public void Update(float weight, DateOnly measuredAt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(weight);
        if (measuredAt == default)
            throw new ArgumentException($"Measured date must be defined. Received: {measuredAt}", nameof(measuredAt));
        Weight = weight;
        MeasuredAt = measuredAt;
    }

}
