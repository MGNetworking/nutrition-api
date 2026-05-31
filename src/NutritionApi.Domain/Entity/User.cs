namespace NutritionApi.Domain.Entity;
using Enums;

public class User
{
    public Guid Id { get; init; }
    public string KeycloakId { get; init; } = null!;
    public DateOnly BirthDate { get; private set; }
    public Gender Gender { get; private set; } = Gender.Unknown;
    public ActivityLevel ActivityLevel { get; private set; } = ActivityLevel.Unknown;
    public float Height { get; private set; }
    public List<Allergen> Allergies { get; private set; } = new List<Allergen>();
    public List<string> DietaryPreferences { get; private set; } = new List<string>();
    public SubscriptionTier SubscriptionTier { get; private set; } = SubscriptionTier.Free;
    public DateTime CreatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    // Pour EF Core uniquement — vide, EF remplit tout via réflexion
    private User() { }

    public User(
        string keycloakId,
        DateOnly birthDate,
        Gender gender,
        ActivityLevel activityLevel,
        float height,
        List<Allergen> allergies,
        List<string> dietaryPreferences)
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

    public void ChangeBirthDate(DateOnly birthDate)
    {
        if (birthDate >= DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException($"Birth date cannot be in the future. Received: {birthDate}", nameof(birthDate));
        BirthDate = birthDate;
    }

    public void ChangeGender(Gender gender)
    {
        if (gender == Gender.Unknown)
            throw new ArgumentException($"Gender must be defined. Received: {gender}", nameof(gender));
        Gender = gender;
    }

    public void ChangeActivityLevel(ActivityLevel activityLevel)
    {
        if (activityLevel == ActivityLevel.Unknown)
            throw new ArgumentException($"Activity level must be defined. Received: {activityLevel}", nameof(activityLevel));
        ActivityLevel = activityLevel;
    }

    public void ChangeHeight(float height)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        Height = height;
    }

    private void SetListAllergen(List<Allergen> allergies)
    {
        ArgumentNullException.ThrowIfNull(allergies);
        Allergies = allergies;
    }

    public void AddAllergen(Allergen allergen)
    {
        if (Allergies.Contains(allergen))
            throw new ArgumentException($"Allergen is already registered. Received: {allergen}", nameof(allergen));
        Allergies.Add(allergen);
    }

    public void RemoveAllergen(Allergen allergen)
    {
        if (!Allergies.Contains(allergen))
            throw new ArgumentException($"Allergen is not registered. Received: {allergen}", nameof(allergen));
        Allergies.Remove(allergen);
    }

    private void SetDietaryPreference(List<string> dietaryPreferences)
    {
        ArgumentNullException.ThrowIfNull(dietaryPreferences);
        DietaryPreferences = dietaryPreferences;
    }

    public void AddDietaryPreference(string dietaryPreference)
    {
        if (DietaryPreferences.Contains(dietaryPreference))
            throw new ArgumentException($"Dietary preference is already registered. Received: {dietaryPreference}", nameof(dietaryPreference));
        DietaryPreferences.Add(dietaryPreference);
    }

    public void RemoveDietaryPreference(string dietaryPreference)
    {
        if (!DietaryPreferences.Contains(dietaryPreference))
            throw new ArgumentException($"Dietary preference is not registered. Received: {dietaryPreference}", nameof(dietaryPreference));
        DietaryPreferences.Remove(dietaryPreference);
    }

    public void ChangeSubscriptionTier(SubscriptionTier tier)
    {

        if (DeletedAt != null)
            throw new InvalidOperationException("Cannot change subscription tier of a deleted user.");

        if (tier == SubscriptionTier.Unknown)
            throw new ArgumentException("Subscription must be defined.", nameof(tier));

        SubscriptionTier = tier;
        
    }

    public void MarkAsDeleted()
    {
        if (DeletedAt != null)
            throw new InvalidOperationException("User account is already marked for deletion.");
        DeletedAt = DateTime.UtcNow;
    }
}
