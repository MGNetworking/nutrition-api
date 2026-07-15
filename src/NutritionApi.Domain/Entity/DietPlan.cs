using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;

namespace NutritionApi.Domain.Entity;

/// <summary>
/// Agrégat racine — plan alimentaire réutilisable ("le moule"), sans dates ni objectif calorique.
/// Sert de base pour lancer une Diet ; le plan reste modifiable et relançable, sa modification
/// n'affecte jamais les Diet déjà créées (snapshot indépendant).
/// Un plan personnel exige un UserId ; un template partagé (IsTemplate) a UserId null
/// et n'est modifiable que par le rôle admin.
/// </summary>
public class DietPlan
{
    /// <summary>Identifiant interne du plan.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant de l'utilisateur propriétaire — null si le plan est un template partagé.</summary>
    public Guid? UserId { get; init; }

    /// <summary>Nom du plan défini par l'utilisateur — différencie ses plans dans la liste.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>True = template partagé en lecture seule (Pro/Business) ; false = plan personnel.</summary>
    public bool IsTemplate { get; private set; }

    /// <summary>Type de régime (équilibré, keto, méditerranéen…).</summary>
    public DietType DietType { get; private set; } = DietType.Unknown;

    /// <summary>Objectif visé par le plan (perte, maintien, prise de poids).</summary>
    public Goal Goal { get; private set; } = Goal.Unknown;

    /// <summary>Poids cible en kilogrammes.</summary>
    public float TargetWeight { get; private set; }

    /// <summary>Répartition cible des macros en % — la somme fait toujours 100.</summary>
    public MacroDistribution MacroDistribution { get; private set; } = null!;

    // réservé à EF Core uniquement
    private DietPlan() { }

    /// <summary>Crée un plan alimentaire — personnel (UserId obligatoire) ou template partagé (UserId null).</summary>
    /// <param name="userId">Identifiant du propriétaire — obligatoire si personnel, interdit si template.</param>
    /// <param name="name">Nom du plan.</param>
    /// <param name="isTemplate">True pour un template partagé, false pour un plan personnel.</param>
    /// <param name="dietType">Type de régime.</param>
    /// <param name="goal">Objectif du plan.</param>
    /// <param name="targetWeight">Poids cible en kg.</param>
    /// <param name="macroDistribution">Répartition cible des macros.</param>
    /// <exception cref="ArgumentException">userId est null ou vide pour un plan personnel, userId est renseigné pour un template, name est null ou vide, ou dietType / goal vaut Unknown.</exception>
    /// <exception cref="ArgumentNullException">macroDistribution est null.</exception>
    public DietPlan(Guid? userId,
        string name,
        bool isTemplate,
        DietType dietType,
        Goal goal,
        float targetWeight,
        MacroDistribution macroDistribution)
    {
        Id = Guid.NewGuid();
        if (!isTemplate && (userId == null || userId == Guid.Empty))
            throw new ArgumentException("User ID is required for a personal DietPlan.", nameof(userId));
        if (isTemplate && userId != null)
            throw new ArgumentException("A template DietPlan cannot be attached to a user.", nameof(userId));

        if (isTemplate)
            MarkAsTemplate();
        else
            UnmarkAsTemplate();

        UserId = userId;
        Rename(name);
        ChangeDietType(dietType);
        ChangeGoal(goal);

        if (targetWeight > 0f)
            SetTargetWeight(targetWeight);
        else
            this.TargetWeight = targetWeight;

        AdjustMacros(macroDistribution);
    }

    /// <summary>Renomme le plan.</summary>
    /// <param name="name">Nouveau nom du plan.</param>
    /// <exception cref="ArgumentException">name est null ou vide.</exception>
    public void Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        this.Name = name;
    }

    /// <summary>Bascule le plan en template partagé.</summary>
    public void MarkAsTemplate()
    {
        this.IsTemplate = true;
    }

    /// <summary>Bascule le plan en plan personnel.</summary>
    public void UnmarkAsTemplate()
    {
        this.IsTemplate = false;
    }


    /// <summary>Modifie le type de régime.</summary>
    /// <param name="dietType">Nouveau type de régime.</param>
    /// <exception cref="ArgumentException">dietType vaut Unknown.</exception>
    public void ChangeDietType(DietType dietType)
    {
        if (dietType == DietType.Unknown)
            throw new ArgumentException($"Diet type must be defined. Received: {dietType}", nameof(dietType));
        this.DietType = dietType;
    }

    /// <summary>Modifie l'objectif du plan.</summary>
    /// <param name="goal">Nouvel objectif.</param>
    /// <exception cref="ArgumentException">goal vaut Unknown.</exception>
    public void ChangeGoal(Goal goal)
    {
        if (goal == Goal.Unknown)
            throw new ArgumentException($"Goal must be defined. Received: {goal}", nameof(goal));
        this.Goal = goal;
    }

    /// <summary>Modifie le poids cible en kilogrammes.</summary>
    /// <param name="targetWeight">Nouveau poids cible en kg.</param>
    /// <exception cref="ArgumentOutOfRangeException">targetWeight est négatif ou nul.</exception>
    public void SetTargetWeight(float targetWeight)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetWeight);
        this.TargetWeight = targetWeight;
    }

    /// <summary>Modifie la répartition cible des macros.</summary>
    /// <param name="macroDistribution">Nouvelle répartition des macros.</param>
    /// <exception cref="ArgumentNullException">macroDistribution est null.</exception>
    public void AdjustMacros(MacroDistribution macroDistribution)
    {
        ArgumentNullException.ThrowIfNull(macroDistribution);
        this.MacroDistribution = macroDistribution;
    }
}
