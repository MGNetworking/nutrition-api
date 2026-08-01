namespace NutritionApi.Domain.Entity;

using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

/// <summary>
/// Agrégat racine — régime réellement en cours, créé au lancement d'un DietPlan.
/// Snapshot complet et indépendant : les attributs sont copiés du plan et le CalorieTarget
/// est calculé au lancement — une Diet lancée n'est plus modifiable, seul son statut évolue.
/// Un User ne peut avoir qu'une seule Diet Active à la fois (garde appliquée en couche Application).
/// </summary>
public class Diet
{
    /// <summary>Identifiant interne du régime.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant de l'utilisateur propriétaire.</summary>
    public Guid UserId { get; init; }

    /// <summary>Nom du régime — copié depuis le DietPlan au lancement.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Type de régime (équilibré, keto, méditerranéen…) — copié depuis le DietPlan.</summary>
    public DietType DietType { get; private set; }

    /// <summary>Objectif de l'utilisateur pour cette période — porté par la Diet, pas par le User.</summary>
    public Goal Goal { get; private set; }

    /// <summary>Poids cible en kilogrammes — copié depuis le DietPlan.</summary>
    public float TargetWeight { get; private set; }

    /// <summary>Objectif calorique calculé au lancement (BMR/TDEE + Goal + WeightEntry le plus récent) — snapshot, ne change jamais.</summary>
    public int CalorieTarget { get; private set; }

    /// <summary>Répartition cible des macros en % — copiée depuis le DietPlan.</summary>
    public MacroDistribution MacroDistribution { get; private set; } = null!;

    /// <summary>Statut du régime — Active à la création, puis Archived ou Cancelled.</summary>
    public DietStatus StatusDiet { get; private set; } = DietStatus.Unknown;

    /// <summary>Date de début — imposée par le système au lancement (date du jour).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Date de fin — null tant que le régime est en cours, fixée automatiquement à l'archivage ou l'annulation.</summary>
    public DateOnly? EndDate { get; private set; }

    // Pour EF Core uniquement
    private Diet() { }

    /// <summary>Crée une Diet Active à partir d'un snapshot de DietPlan — StartDate est fixée à la date du jour (UTC).</summary>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire.</param>
    /// <param name="name">Nom copié depuis le DietPlan.</param>
    /// <param name="dietType">Type de régime copié depuis le DietPlan.</param>
    /// <param name="goal">Objectif copié depuis le DietPlan.</param>
    /// <param name="targetWeight">Poids cible en kg copié depuis le DietPlan.</param>
    /// <param name="calorieTarget">Objectif calorique calculé au lancement par le service.</param>
    /// <param name="macroDistribution">Répartition cible des macros copiée depuis le DietPlan.</param>
    /// <exception cref="ArgumentException">userId est un Guid vide, name est null ou vide, ou dietType / goal vaut Unknown.</exception>
    /// <exception cref="ArgumentOutOfRangeException">targetWeight ou calorieTarget est négatif ou nul.</exception>
    /// <exception cref="ArgumentNullException">macroDistribution est null.</exception>
    public Diet(Guid userId,
        string name,
        DietType dietType,
        Goal goal,
        float targetWeight,
        int calorieTarget,
        MacroDistribution macroDistribution)
    {
        Id = Guid.NewGuid();

        if (userId == Guid.Empty)
            throw new ArgumentException($"User ID cannot be empty. Received: {userId}", nameof(userId));
        UserId = userId;

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;

        if (dietType == DietType.Unknown)
            throw new ArgumentException($"Diet type must be defined. Received: {dietType}", nameof(dietType));
        DietType = dietType;

        if (goal == Goal.Unknown)
            throw new ArgumentException($"Goal must be defined. Received: {goal}", nameof(goal));
        Goal = goal;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetWeight);
        TargetWeight = targetWeight;

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(calorieTarget);
        CalorieTarget = calorieTarget;

        ArgumentNullException.ThrowIfNull(macroDistribution);
        MacroDistribution = macroDistribution;

        StatusDiet = DietStatus.Active;
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow);
    }

    /// <summary>Change le statut du régime — fixe <see cref="EndDate"/> automatiquement quand le statut passe à Archived ou Cancelled.</summary>
    /// <param name="dietStatus">Nouveau statut du régime.</param>
    /// <exception cref="ArgumentException">dietStatus vaut Unknown.</exception>
    /// <exception cref="InvalidOperationException">Le régime est déjà archivé.</exception>
    public void ChangeDietStatus(DietStatus dietStatus)
    {
        if (dietStatus == DietStatus.Unknown)
            throw new ArgumentException($"Diet status cannot be Unknown. Received: {dietStatus}", nameof(dietStatus));
        if (StatusDiet == DietStatus.Archived)
            throw new InvalidOperationException("An archived diet cannot be modified.");
        if (dietStatus == DietStatus.Archived || dietStatus == DietStatus.Cancelled)
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow);

        StatusDiet = dietStatus;
    }
}
