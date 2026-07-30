namespace NutritionApi.Integration.Tests.Persistence;

using Microsoft.EntityFrameworkCore;
using Npgsql;
using NutritionApi.Application.Interfaces.Repositories;
using NutritionApi.Domain.Entity;
using NutritionApi.Domain.Enums;
using NutritionApi.Domain.ValueObjects;
using NutritionApi.Integration.Tests.Fixtures;

/// <summary>
/// IT-EXT-01 à IT-EXT-04 — les repositories contre un PostgreSQL réel (sous-tâche NTR-72).
/// </summary>
/// <remarks>
/// Ce que ce niveau ajoute aux tests unitaires : les requêtes sont réellement traduites en SQL et
/// exécutées. La convention snake_case, les colonnes <c>text[]</c> avec leur <c>ValueComparer</c> et
/// les cascades ne sont vérifiables que là — c'est pourquoi ni InMemory ni SQLite ne sont utilisés.
/// </remarks>
[Trait("Level", "3")]
[Collection(IntegrationCollection.Name)]
public sealed class RepositoryTest(IntegrationFactory factory)
{
    /// <summary>
    /// IT-EXT-01 — <c>UserRepository.GetByKeycloakIdAsync</c> retrouve un utilisateur existant.
    /// </summary>
    [Fact]
    public async Task IT_EXT_01_GetByKeycloakIdAsync_retrouve_l_utilisateur()
    {
        var keycloakId = $"it-ext-01-{Guid.NewGuid()}";
        await SeedUserAsync(keycloakId);

        var (scope, repository) = factory.Resolve<IUserRepository>();
        using (scope)
        {
            var user = await repository.GetByKeycloakIdAsync(keycloakId);

            Assert.NotNull(user);
            Assert.Equal(keycloakId, user.KeycloakId);
        }
    }

    /// <summary>
    /// IT-EXT-02 — <c>DietPlanRepository.GetByUserIdAsync</c> ne retourne que les plans du
    /// propriétaire.
    /// </summary>
    /// <remarks>
    /// Un second utilisateur, doté de son propre plan, est semé volontairement : sans lui, un filtre
    /// <c>WHERE</c> absent passerait inaperçu.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_02_GetByUserIdAsync_filtre_sur_le_proprietaire()
    {
        var owner = await SeedUserAsync($"it-ext-02-owner-{Guid.NewGuid()}");
        var other = await SeedUserAsync($"it-ext-02-other-{Guid.NewGuid()}");

        await using (var context = factory.NewContext())
        {
            context.DietPlans.Add(NewPlan(owner.Id, "Plan du propriétaire"));
            context.DietPlans.Add(NewPlan(other.Id, "Plan d'un tiers"));

            await context.SaveChangesAsync();
        }

        var (scope, repository) = factory.Resolve<IDietPlanRepository>();
        using (scope)
        {
            var plans = await repository.GetByUserIdAsync(owner.Id);

            Assert.Single(plans);
            Assert.Equal("Plan du propriétaire", plans[0].Name);
        }
    }

    /// <summary>
    /// IT-EXT-03 — <c>DietRepository.GetActiveByUserIdAsync</c> ne retourne que le régime actif.
    /// </summary>
    /// <remarks>
    /// L'utilisateur porte aussi un régime archivé : le filtre sur <c>StatusDiet</c> est traduit en
    /// SQL, sur une colonne stockée en entier — ce que seul un vrai PostgreSQL confirme.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_03_GetActiveByUserIdAsync_ecarte_les_regimes_archives()
    {
        var user = await SeedUserAsync($"it-ext-03-{Guid.NewGuid()}");

        await using (var context = factory.NewContext())
        {
            var archived = NewDiet(user.Id, "Régime archivé");
            archived.ChangeDietStatus(DietStatus.Archived);

            context.Diets.Add(archived);
            context.Diets.Add(NewDiet(user.Id, "Régime actif"));

            await context.SaveChangesAsync();
        }

        var (scope, repository) = factory.Resolve<IDietRepository>();
        using (scope)
        {
            var diet = await repository.GetActiveByUserIdAsync(user.Id);

            Assert.NotNull(diet);
            Assert.Equal("Régime actif", diet.Name);
            Assert.Equal(DietStatus.Active, diet.StatusDiet);
        }
    }

    /// <summary>
    /// IT-EXT-04 — les migrations EF Core s'appliquent sur un schéma vierge et créent toutes les
    /// tables.
    /// </summary>
    /// <remarks>
    /// Une base neuve est créée pour ce seul cas : la base de la collection est déjà migrée, elle ne
    /// prouverait donc rien d'un schéma vierge. Les noms attendus sont en snake_case au pluriel,
    /// conformément à la convention appliquée par le contexte.
    /// </remarks>
    [Fact]
    public async Task IT_EXT_04_Les_migrations_s_appliquent_sur_un_schema_vierge()
    {
        await using var database = await TestDatabase.CreateAsync();

        string[] attendues =
        [
            "users", "diet_plans", "diets", "meals", "meal_items",
            "weight_entries", "food_items", "saved_food_items"
        ];

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();

        await using var command = new NpgsqlCommand(
            "select table_name from information_schema.tables where table_schema = 'public';",
            connection);

        var presentes = new List<string>();

        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
                presentes.Add(reader.GetString(0));
        }

        Assert.Empty(attendues.Except(presentes));
    }

    /// <summary>Insère un utilisateur et le retourne.</summary>
    /// <param name="keycloakId">Identifiant Keycloak — unique en base, donc unique par test.</param>
    private async Task<User> SeedUserAsync(string keycloakId)
    {
        await using var context = factory.NewContext();

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

    private static DietPlan NewPlan(Guid userId, string name)
        => new(userId, name, isTemplate: false, DietType.Balanced, Goal.Maintenance, 75f, new MacroDistribution(30, 40, 30));

    private static Diet NewDiet(Guid userId, string name)
        => new(userId, name, DietType.Balanced, Goal.Maintenance, 75f, 2000, new MacroDistribution(30, 40, 30));
}
