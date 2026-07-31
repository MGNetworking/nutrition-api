# Guide xUnit — Tests unitaires .NET

## Sommaire

1. [Configuration projet](#1-configuration-projet)
2. [Anatomie d'un test](#2-anatomie-dun-test)
3. [Attributs xUnit](#3-attributs-xunit)
4. [Moq — Simuler les dépendances](#4-moq--simuler-les-dépendances)
5. [Cycle de vie des tests](#5-cycle-de-vie-des-tests)
6. [Fixtures partagées](#6-fixtures-partagées)
7. [Tests asynchrones](#7-tests-asynchrones)
8. [Exceptions attendues](#8-exceptions-attendues)
9. [MockBehavior — Loose vs Strict](#9-mockbehavior--loose-vs-strict)
10. [Vérifier les appels avec Verify](#10-vérifier-les-appels-avec-verify)
11. [Testcontainers — Tests d'intégration](#11-testcontainers--tests-dintégration)
12. [Bonnes pratiques](#12-bonnes-pratiques)

---

## 1. Configuration projet

### Packages NuGet requis

```xml
<!-- tests/NutritionApi.Application.Tests/NutritionApi.Application.Tests.csproj -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.9.*" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
  <PackageReference Include="Moq" Version="4.20.*" />
  <PackageReference Include="FluentAssertions" Version="6.*" />           <!-- optionnel -->
  <PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />  <!-- intégration -->
</ItemGroup>
```

### Structure de projet recommandée

Le projet distingue **quatre niveaux de tests**. Le rangement les sépare, et un marqueur les rend
sélectionnables.

```
tests/
├── NutritionApi.Domain.Tests/               niveau 1
├── NutritionApi.Application.Tests/          niveau 1
├── NutritionApi.Infrastructure.Tests/       niveau 1 — code pur uniquement
├── NutritionApi.Api.Tests/
│   ├── Level1/                              controllers et middlewares isolés
│   └── Level2/                              pipeline HTTP, doublures aux frontières
│       └── Fixtures/
└── NutritionApi.ExternalIntegration.Tests/  niveau 3 — PostgreSQL, Redis, Keycloak réels
    └── Fixtures/
```

**Chaque classe de test porte son niveau :**

```csharp
[Trait("Level", "1")]
public class UserServiceTest { … }
```

C'est ce marqueur qui pilote les filtres, pas le dossier :

```bash
dotnet test --filter "Level=1"     # unitaires
dotnet test --filter "Level=3"     # exige la pile docker-compose
dotnet test --filter "Level!=3"    # tout ce qui tourne sans Docker
```

Le dossier dit où ranger, le marqueur dit quoi exécuter. Les deux doivent concorder, mais c'est le
marqueur qui fait foi : un test mal rangé reste correctement filtré.

> **Le niveau 3 ne vit pas dans `Infrastructure.Tests`**, malgré les apparences. Ses fixtures
> reposent sur `WebApplicationFactory<Program>`, donc sur la couche API : les y placer obligerait le
> projet de tests d'Infrastructure à référencer l'API, inversant la dépendance que l'architecture
> tient. D'où un projet distinct.

### Configuration xUnit (xunit.runner.json)

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "diagnosticMessages": false,
  "methodDisplay": "method"
}
```

Placer dans le projet de test et configurer en `Content / Copy if newer`.

---

## 2. Anatomie d'un test

### Pattern AAA (Arrange / Act / Assert)

```csharp
[Fact]
public async Task CreateAsync_ShouldReturnDietPlanResponse_WhenRequestIsValid()
{
    // Arrange — préparer les données et les mocks
    var user = new User(...);
    _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);

    // Act — exécuter la méthode testée
    var result = await _dietPlansService.CreateAsync(user.Id, request);

    // Assert — vérifier le résultat
    Assert.NotNull(result);
    Assert.Equal("Mon plan", result.Name);
}
```

### Nommage des tests

**Niveau 1** — `Méthode_Résultat_Condition`. Le test nomme la **méthode** qu'il éprouve.

```
CreateAsync_ShouldReturnDietPlanResponse_WhenRequestIsValid
CreateAsync_ShouldThrow_WhenUserIdIsEmpty
GetByIdAsync_ShouldReturnNull_WhenPlanDoesNotExist
```

**Niveaux 2 et 3** — `IT_XXX_NN_Scénario_RésultatAttendu`. Le test nomme un **cas du recensement**,
pas une méthode : plusieurs classes de production peuvent être traversées.

```
IT_ADM_01_SansRoleAdmin_Retourne403
IT_USR_08_PeseeDejaEnregistreeALaMemeDate_Retourne409
IT_EXT_14_BaseInjoignable_Retourne503
IT_JOB_02_JobJamaisExecute_RetourneScheduled
```

Le préfixe est l'identifiant du recensement : c'est la clé de correspondance entre le test et le cas
recensé. Sans lui, plus rien ne relie les deux.

**Le résultat attendu termine toujours le nom.** `Retourne403`, `LeveConflictException`,
`RetourneListeVide` — un rapport de test doit se lire sans ouvrir les fichiers.

---

## 3. Attributs xUnit

### `[Fact]` — test sans paramètre

```csharp
[Fact]
public void Add_ShouldReturnSum()
{
    Assert.Equal(5, 2 + 3);
}
```

### `[Theory]` + `[InlineData]` — test paramétré inline

```csharp
[Theory]
[InlineData(30, 40, 30, true)]   // somme = 100 → valide
[InlineData(40, 40, 40, false)]  // somme = 120 → invalide
[InlineData(0,  0,  0, false)]   // somme = 0   → invalide
public void MacroDistribution_ShouldValidate(int p, int c, int f, bool expected)
{
    var isValid = (p + c + f) == 100;
    Assert.Equal(expected, isValid);
}
```

### `[Theory]` + `[MemberData]` — données depuis une propriété statique

```csharp
public static IEnumerable<object[]> InvalidMacros =>
[
    [50, 50, 50],
    [0,  0,  0],
    [-1, 50, 51],
];

[Theory]
[MemberData(nameof(InvalidMacros))]
public void MacroDistribution_ShouldThrow_WhenInvalid(int p, int c, int f)
{
    Assert.Throws<ArgumentException>(() => new MacroDistribution(p, c, f));
}
```

### `[Theory]` + `[ClassData]` — données depuis une classe dédiée

```csharp
public class InvalidMacroData : IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return [50, 50, 50];
        yield return [0,  0,  0];
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

[Theory]
[ClassData(typeof(InvalidMacroData))]
public void MacroDistribution_ShouldThrow(int p, int c, int f) { ... }
```

### `[Trait]` — catégoriser les tests

```csharp
[Fact]
[Trait("Category", "UnitTest")]
[Trait("Layer", "Application")]
public void MonTest() { ... }
```

Filtrer à l'exécution :
```bash
dotnet test --filter "Category=UnitTest"
dotnet test --filter "Layer=Application"
```

### `[Skip]` — ignorer temporairement

```csharp
[Fact(Skip = "En attente de NTR-42")]
public void MonTest() { ... }
```

---

## 4. Moq — Simuler les dépendances

### Créer un mock

```csharp
var mock = new Mock<IUserRepository>();       // MockBehavior.Loose (défaut)
var mock = new Mock<IUserRepository>(MockBehavior.Strict);
```

### Setup — définir le comportement

```csharp
// Méthode retournant Task<T>
mock.Setup(r => r.GetByIdAsync(userId))
    .ReturnsAsync(user);

// Méthode retournant Task (void)
mock.Setup(r => r.AddAsync(It.IsAny<User>()))
    .Returns(Task.CompletedTask);  // optionnel en Loose

// Lancer une exception
mock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>()))
    .ThrowsAsync(new NotFoundException());

// Retourner null
mock.Setup(r => r.GetByIdAsync(userId))
    .ReturnsAsync((User?)null);

// Valeur synchrone
mock.Setup(r => r.CountByUserIdAsync(userId))
    .ReturnsAsync(3);
```

### Matchers — `It.Is<T>` et `It.IsAny<T>`

```csharp
// N'importe quelle valeur du type
It.IsAny<Guid>()
It.IsAny<string>()

// Condition spécifique
It.Is<Guid>(id => id != Guid.Empty)
It.Is<string>(s => s.Length > 3)

// Valeur exacte (équivalent sans matcher)
mock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
```

### Accéder à l'objet simulé

```csharp
_service = new DietPlansService(
    _dietPlanRepositoryMock.Object,   // .Object = instance simulée
    _userRepositoryMock.Object,
    _subscriptionGuardMock.Object
);
```

---

## 5. Cycle de vie des tests

### Constructeur + IDisposable

xUnit crée une **nouvelle instance** de la classe de test pour chaque `[Fact]`. Le constructeur joue le rôle de `Setup` (équivalent NUnit/MSTest).

```csharp
public class DietPlansServiceTest : IDisposable
{
    private readonly Mock<IDietPlanRepository> _repoMock = new();
    private readonly DietPlansService _service;

    // Appelé avant chaque test
    public DietPlansServiceTest()
    {
        _service = new DietPlansService(_repoMock.Object);
    }

    // Appelé après chaque test
    public void Dispose()
    {
        // libérer les ressources si nécessaire
    }
}
```

### IAsyncLifetime — setup/teardown asynchrones

```csharp
public class DietPlansServiceTest : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        // Appelé avant chaque test (équivalent constructeur async)
        await _dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        // Appelé après chaque test
        await _dbContext.DisposeAsync();
    }
}
```

---

## 6. Fixtures partagées

### IClassFixture — partagé dans une classe

Utile pour une ressource coûteuse à initialiser (ex : connexion BDD).
Créée **une seule fois** pour toute la classe de test.

```csharp
public class DatabaseFixture : IAsyncLifetime
{
    public AppDbContext DbContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // démarrer le container PostgreSQL une seule fois
        DbContext = await CreateDbContextAsync();
    }

    public Task DisposeAsync() => DbContext.DisposeAsync().AsTask();
}

public class DietPlansRepositoryTest : IClassFixture<DatabaseFixture>
{
    private readonly AppDbContext _db;

    public DietPlansRepositoryTest(DatabaseFixture fixture)
    {
        _db = fixture.DbContext;
    }
}
```

### ICollectionFixture — partagé entre plusieurs classes

```csharp
[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database")]
public class DietPlansRepositoryTest
{
    public DietPlansRepositoryTest(DatabaseFixture fixture) { ... }
}

[Collection("Database")]
public class UserRepositoryTest
{
    public UserRepositoryTest(DatabaseFixture fixture) { ... }
}
```

---

## 7. Tests asynchrones

xUnit supporte nativement `async Task` :

```csharp
[Fact]
public async Task CreateAsync_ShouldReturnPlan()
{
    var result = await _service.CreateAsync(userId, request);
    Assert.NotNull(result);
}
```

Ne jamais utiliser `.Result` ou `.Wait()` — risque de deadlock.

---

## 8. Exceptions attendues

### `Assert.Throws` / `Assert.ThrowsAsync`

```csharp
// Synchrone
var ex = Assert.Throws<ArgumentException>(() => new MacroDistribution(-1, 50, 51));
Assert.Contains("proteinPercentage", ex.ParamName);

// Asynchrone
var ex = await Assert.ThrowsAsync<NotFoundException>(
    () => _service.GetByIdAsync(Guid.Empty));

Assert.Equal("DietPlan not found.", ex.Message);
```

### Vérifier le type uniquement

```csharp
await Assert.ThrowsAsync<ArgumentException>(
    () => _service.CreateAsync(Guid.Empty, request));
```

---

## 9. MockBehavior — Loose vs Strict

| Comportement | Loose (défaut) | Strict |
|---|---|---|
| Méthode sans Setup appelée | Retourne la valeur par défaut | Lance `MockException` |
| `Task` sans Setup | `Task.CompletedTask` | `MockException` |
| `Task<T>` sans Setup | `Task.FromResult(default(T))` | `MockException` |
| `string` sans Setup | `null` | `MockException` |
| `int` sans Setup | `0` | `MockException` |

```csharp
// Loose — comportement permissif (défaut)
var mock = new Mock<IDietPlanRepository>();

// Strict — toute méthode appelée doit avoir un Setup
var mock = new Mock<IDietPlanRepository>(MockBehavior.Strict);
```

**Recommandation :** utiliser `Loose` pour les tests unitaires Application, `Strict` si tu veux t'assurer qu'aucun appel imprévu n'est fait.

---

## 10. Vérifier les appels avec Verify

`Verify` permet de s'assurer qu'une méthode a bien été appelée (ou non).

```csharp
// Vérifier qu'AddAsync a été appelé exactement 1 fois
_dietPlanRepositoryMock.Verify(
    r => r.AddAsync(It.IsAny<DietPlan>()),
    Times.Once);

// Vérifier avec un argument précis
_dietPlanRepositoryMock.Verify(
    r => r.AddAsync(It.Is<DietPlan>(p => p.Name == "Test Plan")),
    Times.Once);

// Vérifier qu'une méthode n'a PAS été appelée
_dietPlanRepositoryMock.Verify(
    r => r.DeleteAsync(It.IsAny<Guid>()),
    Times.Never);

// Vérifier au moins une fois
_dietPlanRepositoryMock.Verify(
    r => r.AddAsync(It.IsAny<DietPlan>()),
    Times.AtLeastOnce);

// Vérifier tous les Setups ont été appelés
_dietPlanRepositoryMock.VerifyAll();
```

### Différence Setup vs Verify

| | Setup | Verify |
|---|---|---|
| Rôle | Définir ce que le mock **retourne** | Contrôler ce qui a été **appelé** |
| Obligatoire | Si la méthode retourne une valeur utilisée | Jamais — c'est une assertion optionnelle |
| Phase AAA | Arrange | Assert |

---

## 11. Testcontainers — Tests d'intégration

> ⚠️ **Non retenu par ce projet.** Les tests d'intégration de niveau 3 s'appuient sur le
> `docker-compose.yml` du projet (PostgreSQL, Redis, Keycloak), réutilisé en CI — décision
> d'architecture du 2026-07-21. Testcontainers a été écarté car des conteneurs isolés ne
> valident ni la configuration Docker, ni le réseau entre composants, ni la chaîne JWT réelle.
> Voir `docs/pages/backend/features/interne/niveaux-de-tests.md`.
>
> Cette section est conservée à titre de **référence xUnit générale**, pas comme la marche à
> suivre du projet.

Testcontainers lance un vrai conteneur Docker pour les tests. Idéal pour tester les repositories EF Core contre PostgreSQL.

```csharp
public class DietPlansRepositoryTest : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("nutrition_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    private AppDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _db = new AppDbContext(options);
        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistDietPlan()
    {
        var plan = new DietPlan(userId, "Plan test", false, DietType.Balanced, Goal.WeightLoss, 75f, macro);

        await _db.DietPlans.AddAsync(plan);
        await _db.SaveChangesAsync();

        var saved = await _db.DietPlans.FindAsync(plan.Id);
        Assert.NotNull(saved);
        Assert.Equal("Plan test", saved.Name);
    }
}
```

---

## 12. Bonnes pratiques

### Un seul comportement testé par `[Fact]`

```csharp
// ✅ — test ciblé
[Fact]
public async Task CreateAsync_ShouldThrow_WhenUserIdIsEmpty()
{
    await Assert.ThrowsAsync<ArgumentException>(
        () => _service.CreateAsync(Guid.Empty, request));
}

// ❌ — trop de comportements dans un seul test
[Fact]
public async Task CreateAsync_Scenarios()
{
    // cas 1
    // cas 2
    // cas 3
}
```

### Ne pas tester les mocks eux-mêmes

```csharp
// ❌ — teste Moq, pas ton code
mock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
var result = await mock.Object.GetByIdAsync(id);
Assert.Equal(user, result);

// ✅ — teste le service qui utilise le mock
mock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(user);
var result = await _service.GetPlanAsync(id);
Assert.Equal(user.Id, result.UserId);
```

### Helpers privés pour les données récurrentes

```csharp
private User CreateUser(string kcId = "kc-123") => new User(
    keycloakId: kcId,
    birthDate: new DateOnly(1990, 1, 1),
    gender: Gender.Male,
    activityLevel: ActivityLevel.LightlyActive,
    height: 180f,
    allergies: [],
    dietaryPreferences: []);

private MacroDistribution DefaultMacros() => new(30, 40, 30);
```

### Réinitialiser les mocks entre les tests

xUnit instancie la classe à chaque test → les mocks `new()` dans les champs sont automatiquement réinitialisés. Pas besoin de `mock.Reset()`.

```csharp
public class DietPlansServiceTest
{
    // Nouveau mock à chaque test — automatique avec xUnit
    private readonly Mock<IDietPlanRepository> _repoMock = new();
}
```

### Ne pas utiliser `Task.Result` dans les tests

```csharp
// ❌ — risque de deadlock
var result = _service.CreateAsync(id, request).Result;

// ✅
var result = await _service.CreateAsync(id, request);
```
