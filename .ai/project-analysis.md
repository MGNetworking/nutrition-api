# project-analysis.md

Structure physique du projet — où se trouve le code, comment il est organisé.

---

## Racines

```
code.root  : src/
tests.root : tests/
```

---

## Couches (src/)

```
NutritionApi.Domain/
  Entities/           ← agrégats, entités, value objects
  Enums/              ← énumérations domaine
  Exceptions/         ← exceptions domaine (si présentes)

NutritionApi.Application/
  Interfaces/
    Services/         ← IXxxService (contrats API → Application)
    Repositories/     ← IXxxRepository (contrats Application → Infrastructure)
    ExternalServices/ ← IXxxService (contrats vers services externes)
  Services/           ← implémentations des services applicatifs
  DTOs/               ← Request / Response DTOs
  Guards/             ← gardes transversaux (ex: SubscriptionGuard)

NutritionApi.Infrastructure/
  Persistence/        ← DbContext, configurations EF Core
  Repositories/       ← implémentations des repositories
  ExternalServices/   ← clients Keycloak, Open Food Facts, Stripe

NutritionApi.Api/
  Controllers/        ← endpoints REST
  Filters/            ← filtres d'exception
  Extensions/         ← DI, middleware
```

---

## Tests (tests/)

```
NutritionApi.Domain.Tests/
NutritionApi.Application.Tests/
NutritionApi.Infrastructure.Tests/
NutritionApi.Api.Tests/
```

---

## Stack

```
Langage         : C# 13
Framework       : ASP.NET Core 10
ORM             : Entity Framework Core 10 + Npgsql
Base de données : PostgreSQL
Auth            : Keycloak (OAuth2 / OIDC)
Résilience      : Polly
Jobs            : Hangfire
OpenAPI         : Swashbuckle
Déploiement     : Kubernetes
```

---

## Framework de test

```
Framework  : xUnit
Mocks      : Moq
Intégration: Testcontainers
```

---

## Conventions

### Architecture
- DDD 4 couches : Domain / Application / Infrastructure / Api
- Workflow obligatoire : Interfaces → API → Application → Infrastructure

### Constructeurs (entités Domain)
- `Id` → `Guid.NewGuid()` dans le constructeur
- `UserId` → validation `Guid.Empty` + assignation dans le constructeur
- Props mutables → délégation à `UpdateXxx()` depuis le constructeur
- Valeurs fixes → assignation directe

### Exceptions (gardes)
- `< 0`  → `ArgumentOutOfRangeException.ThrowIfNegative(x)`
- `<= 0` → `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x)`
- Enum Unknown / Guid.Empty / invariant combiné → `ArgumentException($"... Received: {value}", nameof(value))`

### DTO Response
- Mapping porté par le DTO via `static From(Entity entity)`
- Le service ne mappe pas
- `using NutritionApi.Domain.Entity;` accepté dans un DTO Response
- Le DTO Request ne porte pas de `ToXxx()`

### Commits
- Format : `feat(scope): description courte #NTR-XX`
- Référence `#NTR-XX` obligatoire
- Pas de `Co-Authored-By:`
