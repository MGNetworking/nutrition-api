using NutritionApi.Domain.Enums;

namespace NutritionApi.Domain.Entity;

/// <summary>
/// Agrégat racine — point d'entrée du système. Détient le profil physiologique de l'utilisateur
/// (date de naissance, genre, taille, niveau d'activité, allergies, préférences alimentaires)
/// et possède ses Diet, DietPlan, Meal, WeightEntry et SavedFoodItem.
/// </summary>
public class User
{
    /// <summary>Identifiant interne de l'utilisateur.</summary>
    public Guid Id { get; init; }

    /// <summary>Identifiant Keycloak (claim <c>sub</c> du JWT) — clé de résolution de l'identité interne.</summary>
    public string KeycloakId { get; init; } = null!;

    /// <summary>Date de naissance — l'âge est calculé à la demande, jamais stocké.</summary>
    public DateOnly BirthDate { get; private set; }

    /// <summary>Genre de l'utilisateur — utilisé pour le calcul BMR.</summary>
    public Gender Gender { get; private set; } = Gender.Unknown;

    /// <summary>Niveau d'activité physique — détermine le facteur d'activité du calcul TDEE.</summary>
    public ActivityLevel ActivityLevel { get; private set; } = ActivityLevel.Unknown;

    /// <summary>Taille en centimètres — fixe sur le User.</summary>
    public float Height { get; private set; }

    /// <summary>Allergènes de l'utilisateur (14 allergènes officiels UE) — liste vide = confirmé aucune allergie.</summary>
    public List<Allergen> Allergies { get; private set; } = new List<Allergen>();

    /// <summary>Régimes alimentaires déclarés, non filtrants — liste vide = confirmé aucune préférence.</summary>
    public List<DietaryPreference> DietaryPreferences { get; private set; } = new List<DietaryPreference>();

    /// <summary>Palier d'abonnement — Free par défaut à la création ; source de vérité en base, jamais lue depuis le JWT.</summary>
    public SubscriptionTier SubscriptionTier { get; private set; } = SubscriptionTier.Free;

    /// <summary>Date de création du compte (UTC).</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Null = compte actif ; renseigné = suppression demandée (grace period de 30 jours).</summary>
    public DateTime? DeletedAt { get; private set; }


    // Pour EF Core uniquement — vide, EF remplit tout via réflexion
    private User() { }

    /// <summary>Crée un nouvel utilisateur avec le tier Free et la date de création courante (UTC).</summary>
    /// <param name="keycloakId">Identifiant Keycloak (claim <c>sub</c> du JWT).</param>
    /// <param name="birthDate">Date de naissance — ne peut pas être dans le futur.</param>
    /// <param name="gender">Genre de l'utilisateur.</param>
    /// <param name="activityLevel">Niveau d'activité physique.</param>
    /// <param name="height">Taille en centimètres.</param>
    /// <param name="allergies">Allergènes de l'utilisateur — liste vide = confirmé aucune allergie.</param>
    /// <param name="dietaryPreferences">Préférences alimentaires — liste vide = confirmé aucune préférence.</param>
    /// <exception cref="ArgumentException">keycloakId est null ou vide, birthDate est dans le futur, ou gender / activityLevel vaut Unknown.</exception>
    /// <exception cref="ArgumentOutOfRangeException">height est négatif ou nul.</exception>
    /// <exception cref="ArgumentNullException">allergies ou dietaryPreferences est null.</exception>
    public User(
        string keycloakId,
        DateOnly birthDate,
        Gender gender,
        ActivityLevel activityLevel,
        float height,
        List<Allergen> allergies,
        List<DietaryPreference> dietaryPreferences)
    {
        Id = Guid.NewGuid();
        ArgumentException.ThrowIfNullOrWhiteSpace(keycloakId);
        KeycloakId = keycloakId;

        ChangeBirthDate(birthDate);
        ChangeGender(gender);
        ChangeActivityLevel(activityLevel);
        ChangeHeight(height);
        SetListAllergen(allergies);
        SetDietaryPreference(dietaryPreferences);
        SubscriptionTier = SubscriptionTier.Free;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>Modifie la date de naissance.</summary>
    /// <param name="birthDate">Nouvelle date de naissance.</param>
    /// <exception cref="ArgumentException">birthDate est aujourd'hui ou dans le futur.</exception>
    public void ChangeBirthDate(DateOnly birthDate)
    {
        if (birthDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException($"Birth date cannot be in the future. Received: {birthDate}", nameof(birthDate));
        BirthDate = birthDate;
    }

    /// <summary>Modifie le genre.</summary>
    /// <param name="gender">Nouveau genre.</param>
    /// <exception cref="ArgumentException">gender vaut Unknown.</exception>
    public void ChangeGender(Gender gender)
    {
        if (gender == Gender.Unknown)
            throw new ArgumentException($"Gender must be defined. Received: {gender}", nameof(gender));
        Gender = gender;
    }

    /// <summary>Modifie le niveau d'activité physique.</summary>
    /// <param name="activityLevel">Nouveau niveau d'activité.</param>
    /// <exception cref="ArgumentException">activityLevel vaut Unknown.</exception>
    public void ChangeActivityLevel(ActivityLevel activityLevel)
    {
        if (activityLevel == ActivityLevel.Unknown)
            throw new ArgumentException($"Activity level must be defined. Received: {activityLevel}", nameof(activityLevel));
        ActivityLevel = activityLevel;
    }

    /// <summary>Modifie la taille en centimètres.</summary>
    /// <param name="height">Nouvelle taille en cm.</param>
    /// <exception cref="ArgumentOutOfRangeException">height est négatif ou nul.</exception>
    public void ChangeHeight(float height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Height = height;
    }

    /// <summary>Remplace la liste complète des allergènes.</summary>
    /// <param name="allergies">Nouvelle liste d'allergènes — vide = confirmé aucune allergie.</param>
    /// <exception cref="ArgumentNullException">allergies est null.</exception>
    public void SetListAllergen(List<Allergen> allergies)
    {
        ArgumentNullException.ThrowIfNull(allergies);
        Allergies = allergies;
    }

    /// <summary>Ajoute un allergène à la liste — doublon interdit.</summary>
    /// <param name="allergen">Allergène à ajouter.</param>
    /// <exception cref="ArgumentException">L'allergène est déjà enregistré.</exception>
    public void AddAllergen(Allergen allergen)
    {
        if (Allergies.Contains(allergen))
            throw new ArgumentException($"Allergen is already registered. Received: {allergen}", nameof(allergen));
        Allergies.Add(allergen);
    }

    /// <summary>Retire un allergène de la liste — il doit exister.</summary>
    /// <param name="allergen">Allergène à retirer.</param>
    /// <exception cref="ArgumentException">L'allergène n'est pas enregistré.</exception>
    public void RemoveAllergen(Allergen allergen)
    {
        if (!Allergies.Contains(allergen))
            throw new ArgumentException($"Allergen is not registered. Received: {allergen}", nameof(allergen));
        Allergies.Remove(allergen);
    }

    /// <summary>Remplace la liste complète des préférences alimentaires.</summary>
    /// <param name="dietaryPreferences">Nouvelle liste de préférences — vide = confirmé aucune préférence.</param>
    /// <exception cref="ArgumentNullException">dietaryPreferences est null.</exception>
    public void SetDietaryPreference(List<DietaryPreference> dietaryPreferences)
    {
        ArgumentNullException.ThrowIfNull(dietaryPreferences);
        DietaryPreferences = dietaryPreferences;
    }

    /// <summary>Ajoute une préférence alimentaire — doublon interdit.</summary>
    /// <param name="dietaryPreference">Préférence à ajouter.</param>
    /// <exception cref="ArgumentException">La préférence est déjà enregistrée.</exception>
    public void AddDietaryPreference(DietaryPreference dietaryPreference)
    {
        if (DietaryPreferences.Contains(dietaryPreference))
            throw new ArgumentException($"Dietary preference is already registered. Received: {dietaryPreference}", nameof(dietaryPreference));
        DietaryPreferences.Add(dietaryPreference);
    }

    /// <summary>Retire une préférence alimentaire — elle doit exister.</summary>
    /// <param name="dietaryPreference">Préférence à retirer.</param>
    /// <exception cref="ArgumentException">La préférence n'est pas enregistrée.</exception>
    public void RemoveDietaryPreference(DietaryPreference dietaryPreference)
    {
        if (!DietaryPreferences.Contains(dietaryPreference))
            throw new ArgumentException($"Dietary preference is not registered. Received: {dietaryPreference}", nameof(dietaryPreference));
        DietaryPreferences.Remove(dietaryPreference);
    }

    /// <summary>Change le palier d'abonnement de l'utilisateur.</summary>
    /// <param name="tier">Nouveau palier d'abonnement.</param>
    /// <exception cref="InvalidOperationException">Le compte est marqué pour suppression.</exception>
    /// <exception cref="ArgumentException">tier vaut Unknown.</exception>
    public void ChangeSubscriptionTier(SubscriptionTier tier)
    {

        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot change subscription tier of a deleted user.");

        if (tier == SubscriptionTier.Unknown)
            throw new ArgumentException("Subscription must be defined.", nameof(tier));

        SubscriptionTier = tier;

    }

    /// <summary>Déclenche la suppression douce du compte — fixe <see cref="DeletedAt"/> à maintenant (grace period de 30 jours).</summary>
    /// <exception cref="InvalidOperationException">Le compte est déjà marqué pour suppression.</exception>
    public void MarkAsDeleted()
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("User account is already marked for deletion.");
        DeletedAt = DateTime.UtcNow;
    }

    /// <summary>Réactive un compte marqué pour suppression — remet <see cref="DeletedAt"/> à null.</summary>
    /// <exception cref="InvalidOperationException">Le compte n'est pas marqué pour suppression.</exception>
    public void Reactivate()
    {
        if (DeletedAt is null)
            throw new InvalidOperationException("User is not deleted.");

        DeletedAt = null;
    }
}
