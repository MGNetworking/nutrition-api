namespace NutritionApi.ExternalIntegration.Tests.Persistence;

using System.Net;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NutritionApi.Application.Exceptions;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.ExternalIntegration.Tests.Fixtures;

/// <summary>
/// le branchement de <c>DatabaseExceptionInterceptor</c> sur EF Core.
/// </summary>
/// <remarks>
/// Transférés depuis NTR-135. Sa méthode <c>Translate</c> est déjà couverte par 5 tests unitaires ;
/// ce qui reste à éprouver, c'est qu'EF Core l'invoque au bon moment. <c>AddInterceptors</c> est une
/// configuration, et une configuration ne se vérifie que contre une vraie base.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class DatabaseExceptionTest(IntegrationFactory factory)
{
    /// <summary>
    /// une violation d'unicité remonte en <see cref="ConflictException"/>, pas en erreur
    /// brute.
    /// </summary>
    /// <remarks>
    /// Ce que le test prouve : EF Core appelle bien l'intercepteur sur l'échec de commande, et
    /// celui-ci discrimine sur l'exception <b>interne</b> — la <c>PostgresException 23505</c> qu'EF
    /// Core enveloppe dans une <c>DbUpdateException</c>. Sans le branchement, l'exception Npgsql
    /// remonterait telle quelle et l'API répondrait 500.
    /// <para>
    /// <b>Deux écarts au recensement, assumés.</b> Le cas y est décrit sur deux <c>WeightEntry</c> à
    /// la même date : or <c>weight_entries</c> ne porte aucune contrainte d'unicité sur
    /// <c>(user_id, measured_at)</c> — le doublon serait accepté et le test ne prouverait rien. Il
    /// s'appuie donc sur <c>saved_food_items (user_id, food_item_id)</c>, seule contrainte du schéma
    /// qui exprime le même invariant. Et l'assertion porte sur l'exception plutôt que sur le 409 :
    /// le service applicatif vérifie l'existence avant d'insérer, aucun appel HTTP ne peut donc
    /// atteindre la contrainte. La traduction en 409 est couverte par le filtre d'exceptions.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task SaveChangesAsync_ShouldThrowConflictException_WhenUniqueConstraintIsViolated()
    {
        var user = await SeedUserAsync($"it-ext-13-{Guid.NewGuid()}");
        var foodItem = await SeedFoodItemAsync();

        await using (var context = factory.NewContext())
        {
            context.SavedFoodItems.Add(new SavedFoodItem(user.Id, foodItem.Id));
            await context.SaveChangesAsync();
        }

        await using (var context = factory.NewContext())
        {
            context.SavedFoodItems.Add(new SavedFoodItem(user.Id, foodItem.Id));

            var echec = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            // L'exception traduite ne remonte jamais nue : EF Core encapsule les échecs de commande.
            // C'est cette forme exacte que ExceptionMiddleware déballe pour répondre 409.
            Assert.IsType<ConflictException>(echec.InnerException);
        }
    }

    /// <summary>
    /// base injoignable : l'API répond 503 avec <c>Retry-After</c>, pas 500.
    /// </summary>
    /// <remarks>
    /// Le conteneur PostgreSQL est arrêté puis redémarré par le test lui-même. C'est le seul cas de
    /// la suite qui manipule l'infrastructure : la parallélisation est désactivée pour tout
    /// l'assembly (voir <c>IntegrationCollection</c>), et les pools Npgsql sont purgés en sortie pour
    /// que les tests suivants ne réutilisent pas une connexion morte.
    /// <para>
    /// Ce que le test prouve : le chemin <c>NpgsqlException</c> → <see cref="ServiceUnavailableException"/>
    /// est bien emprunté depuis une commande EF Core réelle.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task GetUsersMe_ShouldReturn503_WhenDatabaseIsUnreachable()
    {
        await SeedUserAsync(KeycloakTokens.StandardUserSubject, ignoreExisting: true);

        using var client = await factory.CreateTokenClientAsync(KeycloakTokens.StandardUser);

        DockerContainer.Stop(DockerContainer.Postgres);

        try
        {
            NpgsqlConnection.ClearAllPools();

            var response = await client.GetAsync("/api/v1/users/me");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.True(
                response.Headers.Contains("Retry-After"),
                "La réponse 503 doit porter un en-tête Retry-After.");
        }
        finally
        {
            DockerContainer.Start(DockerContainer.Postgres);
            await DockerContainer.WaitHealthyAsync(DockerContainer.Postgres, TimeSpan.FromSeconds(120));

            NpgsqlConnection.ClearAllPools();
        }
    }

    private async Task<User> SeedUserAsync(string keycloakId, bool ignoreExisting = false)
    {
        await using var context = factory.NewContext();

        var existing = context.Users.FirstOrDefault(u => u.KeycloakId == keycloakId);

        if (existing is not null && ignoreExisting)
            return existing;

        var user = new User(
            keycloakId: keycloakId,
            birthDate: new DateOnly(1990, 1, 1),
            gender: Gender.Male,
            activityLevel: ActivityLevel.Sedentary,
            height: 180,
            allergies: [],
            dietaryPreferences: []);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }

    private async Task<FoodItem> SeedFoodItemAsync()
    {
        await using var context = factory.NewContext();

        var foodItem = new FoodItem(
            offId: $"it-ext-13-{Guid.NewGuid()}",
            name: "Aliment de test",
            caloriesPer100g: 100f,
            proteinsPer100g: 10,
            carbsPer100g: 10,
            fatsPer100g: 10,
            allergensTags: []);

        context.FoodItems.Add(foodItem);
        await context.SaveChangesAsync();

        return foodItem;
    }
}
