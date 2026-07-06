# Conventions de code & Stack technique

## Stack technique

| Composant | Technologie |
|---|---|
| Backend | C# 13 / ASP.NET Core 10 |
| ORM | Entity Framework Core 10 + Npgsql |
| Base de données | PostgreSQL |
| Auth | Keycloak (OAuth2 / OIDC) |
| Résilience | Polly |
| Jobs planifiés | Hangfire |
| Tests | xUnit + Moq + Testcontainers |
| OpenAPI | Swashbuckle |
| Déploiement | Kubernetes |

Architecture : **DDD 4 couches** — `Domain / Application / Infrastructure / Api`

---

## Workflow API-first — ordre obligatoire

```
Interfaces → API layer → Application layer → Infrastructure layer
```

Avant chaque ticket API : lire la section du controller dans `design-api.md` (table des routes) — pas seulement l'interface. L'interface est un contrat technique, pas la source de vérité des routes.

Trois frontières d'interface :
- API → Application : `IXxxService` dans `Application/Interfaces/Services/`
- Application → Infrastructure : `IXxxRepository` dans `Application/Interfaces/Repositories/`
- Application → Services externes : `IXxxService` dans `Application/Interfaces/ExternalServices/`

---

## Pattern constructeur (entités Domain)

- `Id` → `Guid.NewGuid()` directement dans le constructeur
- `UserId` → validation `Guid.Empty` + assignation directement dans le constructeur
- Propriétés mutables (`Name`, `MealType`…) → déléguer à `Update***()` depuis le constructeur
- Valeurs fixes à la création (`IsSaved = false`, `MealItems = new List<>()`) → assignation directe

La validation n'est écrite qu'une seule fois — dans `Update***` — le constructeur appelle ces méthodes.

---

## Pattern exceptions (gardes)

- `< 0` → `ArgumentOutOfRangeException.ThrowIfNegative(x)`
- `<= 0` → `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x)`
- Enum `== Unknown`, `Guid.Empty`, invariant combiné → `ArgumentException($"... Received: {value}", nameof(value))`

---

## Pattern DTO Response

Le DTO Response porte son propre mapping via `static From(Entity entity)`. Le service ne mappe pas.

```csharp
// ✅ Sur le DTO Response
public static UserProfileResponse From(User user) { ... }

// ❌ Pas dans le service
var dto = new UserProfileResponse { ... };
```

Le DTO Request ne porte pas de `ToXxx()` — le service extrait les primitives lui-même.
`using NutritionApi.Domain.Entity;` dans un DTO Response est accepté.
