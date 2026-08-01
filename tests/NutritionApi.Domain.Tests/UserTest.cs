using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;

namespace NutritionApi.Domain.Tests;

[Trait("Level", "1")]
public class UserTest
{
    float Height = 100;
    public User CreateUser(List<Allergen>? allergens = null, List<DietaryPreference>? preferences = null)
    {
        return new User(
            keycloakId: "test-user",
            birthDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            Height,
            allergies: allergens ?? new List<Allergen>(),
            dietaryPreferences: preferences ?? new List<DietaryPreference>());
    }

    public static IEnumerable<object[]> FutureDates =>
        [
            [DateOnly.FromDateTime(DateTime.UtcNow)],
            [DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1))]
        ];

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void keycloakId_ThrowIfNullOrWhiteSpaceTest(string? kcId)
    {
        Assert.ThrowsAny<ArgumentException>(() => new User(
            keycloakId: kcId,
            birthDate: DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25)),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            Height,
            allergies: new List<Allergen>(),
            dietaryPreferences: new List<DietaryPreference>())
        );
    }

    [Theory]
    [MemberData(nameof(FutureDates))]
    public void ChangeBirthDate_ArgumentExceptionTest(DateOnly dt)
    {
        Assert.Throws<ArgumentException>(() => CreateUser().ChangeBirthDate(dt));
    }

    [Fact]
    public void ChangeBirthDate_OkTest()
    {
        User user = CreateUser();
        DateTime dt = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        DateOnly birthDay = DateOnly.FromDateTime(dt);

        user.ChangeBirthDate(birthDay);
        Assert.Equal(birthDay, user.BirthDate);

    }

    [Fact]
    public void ChangeGender_ArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateUser().ChangeGender(Gender.Unknown));
    }

    [Fact]
    public void ChangeGender_OkTest()
    {
        User user = CreateUser(); // créé avec Gender.Male
        user.ChangeGender(Gender.Female);
        Assert.Equal(Gender.Female, user.Gender);
    }

    [Fact]
    public void ChangeActivityLevel_ArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateUser().ChangeActivityLevel(ActivityLevel.Unknown));
    }

    [Fact]
    public void ChangeActivityLevel_OkTest()
    {
        User user = CreateUser();
        user.ChangeActivityLevel(ActivityLevel.VeryActive);
        Assert.Equal(ActivityLevel.VeryActive, user.ActivityLevel);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(-1.0f)]
    public void ChangeHeight_ArgumentOutOfRangeExceptionTest(float height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateUser().ChangeHeight(height));
    }

    [Fact]
    public void ChangeHeight_OkTest()
    {
        User user = CreateUser();
        user.ChangeHeight(90f);
        Assert.Equal(90f, user.Height);
    }


    [Fact]
    public void SetListAllergen_ArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => CreateUser().SetListAllergen(null!));
    }

    [Fact]
    public void SetListAllergen_OkTest()
    {
        var allergies = new List<Allergen> { Allergen.Gluten };
        User user = CreateUser();
        user.SetListAllergen(allergies);
        Assert.Equal(allergies, user.Allergies);

    }

    [Fact]
    public void AddAllergen_ThrowArgumentExceptionTest()
    {
        User user = CreateUser();
        user.AddAllergen(Allergen.Gluten);

        Assert.Throws<ArgumentException>(() => user.AddAllergen(Allergen.Gluten));
    }

    [Fact]
    public void AddAllergen_OKTest()
    {
        User user = CreateUser();
        user.AddAllergen(Allergen.Gluten);
        Assert.Contains(Allergen.Gluten, user.Allergies);
    }

    [Fact]
    public void RemoveAllergen_OkTest()
    {
        var allergen = Allergen.Gluten;
        User user = CreateUser(allergens: new List<Allergen> { allergen });

        user.RemoveAllergen(allergen);
        Assert.DoesNotContain(allergen, user.Allergies);
    }

    [Fact]
    public void RemoveAllergen_ArgumentExceptionTest()
    {
        User user = CreateUser(allergens: new List<Allergen> { Allergen.Gluten });
        Assert.Throws<ArgumentException>(() => user.RemoveAllergen(Allergen.Peanuts));
    }


    [Fact]
    public void ChangeSubscriptionTier_OkTest()
    {
        User user = CreateUser();
        user.ChangeSubscriptionTier(SubscriptionTier.Pro);
        Assert.Equal(SubscriptionTier.Pro, user.SubscriptionTier);
    }

    [Fact]
    public void ChangeSubscriptionTier_InvalidOperationExceptionTest()
    {
        User user = CreateUser();
        user.MarkAsDeleted();
        Assert.Throws<InvalidOperationException>(() => user.ChangeSubscriptionTier(SubscriptionTier.Free));

    }

    [Fact]
    public void ChangeSubscriptionTier_ArgumentExceptionTest()
    {
        Assert.Throws<ArgumentException>(() => CreateUser().ChangeSubscriptionTier(SubscriptionTier.Unknown));
    }

    [Fact]
    public void SetDietaryPreference_ArgumentNullExceptionTest()
    {
        Assert.Throws<ArgumentNullException>(() => CreateUser().SetDietaryPreference(null!));
    }

    [Fact]
    public void SetDietaryPreference_OkTest()
    {
        var preferences = new List<DietaryPreference> { DietaryPreference.Vegan };
        User user = CreateUser();
        user.SetDietaryPreference(preferences);
        Assert.Equal(preferences, user.DietaryPreferences);
    }

    [Fact]
    public void AddDietaryPreference_ThrowArgumentExceptionTest()
    {
        DietaryPreference prefer = DietaryPreference.Halal;
        User user = CreateUser();
        user.AddDietaryPreference(prefer);
        Assert.Throws<ArgumentException>(() => user.AddDietaryPreference(prefer));
    }


    [Fact]
    public void AddDietaryPreference_OkTest()
    {
        DietaryPreference prefer = DietaryPreference.Halal;
        User user = CreateUser();
        user.AddDietaryPreference(prefer);
        Assert.Contains(prefer, user.DietaryPreferences);
    }

    [Fact]
    public void RemoveDietaryPreference_ThrowArgumentExceptionTest()
    {
        DietaryPreference prefer = DietaryPreference.Vegetarian;
        User user = CreateUser();
        user.AddDietaryPreference(prefer);
        Assert.Throws<ArgumentException>(() => user.RemoveDietaryPreference(DietaryPreference.Vegan));
    }

    [Fact]
    public void RemoveDietaryPreference_OkTest()
    {
        DietaryPreference prefer = DietaryPreference.Vegan;
        User user = CreateUser(preferences: new List<DietaryPreference> { prefer });
        user.RemoveDietaryPreference(prefer);
        Assert.DoesNotContain(prefer, user.DietaryPreferences);
    }

    [Fact]
    public void MarkAsDeleted_ThrowInvalidOperationExceptionTest()
    {

        User user = CreateUser();
        user.MarkAsDeleted();
        Assert.Throws<InvalidOperationException>(() => user.MarkAsDeleted());
    }

    [Fact]
    public void MarkAsDeleted_OKTest()
    {
        User user = CreateUser();
        user.MarkAsDeleted();

        Assert.NotNull(user.DeletedAt);
    }


    [Fact]
    public void Reactivate_ThrowInvalidOperationExceptionTest()
    {

        User user = CreateUser();
        Assert.Throws<InvalidOperationException>(() => user.Reactivate());
    }

    [Fact]
    public void Reactivate_OKTest()
    {
        User user = CreateUser();
        user.MarkAsDeleted();
        user.Reactivate();
        Assert.Null(user.DeletedAt);
    }


    [Fact]
    public void Constructor_DeletedAt_IsNullByDefaultTest()
    {
        User user = CreateUser();
        Assert.True(user.DeletedAt == null);
    }

}
