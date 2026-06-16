# HARNESS.md — nutrition-api

Point d'entrée universel pour tous les agents (Claude Code, Codex, Gemini…).

---

## Sources de vérité

Racine de la documentation : `../docs/pages/backend/`

### Design (architecture)

| Fichier | Contenu |
|---|---|
| `design/design-domain.md` | Modèle domaine, agrégats, invariants |
| `design/design-application.md` | Couche Application — patterns, interfaces, DTOs |
| `design/design-infrastructure.md` | Couche Infrastructure — EF Core, Redis, Hangfire |
| `design/design-api.md` | Couche API — controllers, routing, auth JWT, **table des routes** |
| `design/Regles-metier.md` | Formules BMR/TDEE + invariants domaine |
| `design/regles-metier-consolidees.md` | Toutes les règles métier par entité — référence unique |

### Backlog et implémentation

| Fichier | Contenu |
|---|---|
| `livrable/checklist-implementation.md` | Tous les items par couche (Domain / Application / Infrastructure / API) |
| `livrable/specs-frontend.md` | 8 écrans + contrats API par écran |
| `features/` | Un fichier par feature (diet.md, repas.md, aliments.md…) |

### Annexes

| Fichier | Contenu |
|---|---|
| `annexes/Diagramme-classes.md` | Diagramme Mermaid du modèle domaine |
| `annexes/infrastructure-hangfire.md` | Jobs Hangfire |
| `annexes/infrastructure-import-off.md` | Import Open Food Facts quotidien |
| `annexes/infrastructure-keycloak-admin.md` | Keycloak admin |
| `annexes/concept-moteur-architecture.md` | Concept "Moteur (Engine)" — Strategy + Factory, `NutritionCalculator` |

### Features

| Fichier | Contenu |
|---|---|
| `features/nutrition-calculator.md` | Contrat du Moteur de calcul nutritionnel — méthodes, formules BMR, `MacroGrams`, `GetDefaultMacros` |

---

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

## Conventions de code

### Workflow API-first — ordre obligatoire

```
Interfaces → API layer → Application layer → Infrastructure layer
```

Avant chaque ticket API : lire la section du controller dans `design-api.md` (table des routes) — pas seulement l'interface.

Trois frontières d'interface :
- API → Application : `IXxxService` dans `Application/Interfaces/Services/`
- Application → Infrastructure : `IXxxRepository` dans `Application/Interfaces/Repositories/`
- Application → Services externes : `IXxxService` dans `Application/Interfaces/ExternalServices/`

### Pattern constructeur (entités Domain)

- `Id` → `Guid.NewGuid()` directement dans le constructeur
- `UserId` → validation `Guid.Empty` + assignation directement dans le constructeur
- Propriétés mutables (`Name`, `MealType`…) → déléguer à `Update***()` depuis le constructeur
- Valeurs fixes à la création (`IsSaved = false`, `MealItems = new List<>()`) → assignation directe

La validation n'est écrite qu'une seule fois — dans `Update***` — le constructeur appelle ces méthodes.

### Pattern exceptions (gardes)

- `< 0` → `ArgumentOutOfRangeException.ThrowIfNegative(x)`
- `<= 0` → `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x)`
- Enum `== Unknown`, `Guid.Empty`, invariant combiné → `ArgumentException($"... Received: {value}", nameof(value))`

### Pattern DTO Response

Le DTO Response porte son propre mapping via `static From(Entity entity)`. Le service ne mappe pas.

```csharp
// ✅ Sur le DTO Response
public static UserProfileResponse From(User user) { ... }

// ❌ Pas dans le service
var dto = new UserProfileResponse { ... };
```

Le DTO Request ne porte pas de `ToXxx()` — le service extrait les primitives lui-même.
`using NutritionApi.Domain.Entity;` dans un DTO Response est accepté.

---

## État courant du projet

**Branche active :** `feature/NTR-2-application-layer`
**Epic en cours :** NTR-2 — Application Layer
**Prochaine tâche :** NTR-38 et NTR-39 — sous-tâches de NTR-9 (DietPlansService)

Tickets terminés récemment :
- NTR-37 ✅ — WeightEntry dans UserService
- NTR-8 ✅ — RgpdController + RgpdService découplé de UserService

Travail réalisé (NTR-9) :
- `DietPlansService` — 6 méthodes implémentées
- `SubscriptionGuard` créée et testée
- Moteur de calcul nutritionnel (`NutritionCalculator`, `NutritionCalculatorFactory`, stratégies BMR)
- `regles-metier-consolidees.md`, `features/nutrition-calculator.md`, `annexes/concept-moteur-architecture.md` créés

Ordre des Epics :
```
NTR-79 ✅ → NTR-4 API Layer ✅ → NTR-2 Application (en cours) → NTR-3 Infrastructure
```
