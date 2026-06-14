# CLAUDE.md — nutrition-api

> Fichier de référence Claude Code pour ce projet. Il prime sur tout comportement par défaut.
> La documentation complète du projet est dans le répertoire parent — voir la section "Fichiers clés".

---

## Règles de collaboration — à lire en premier

Ces règles s'appliquent sans exception à toutes les sessions.

### Mise à jour mémoire — toujours les deux

Quand Maxime demande de "mettre en mémoire", "noter" ou "mettre à jour" une information, mettre à jour **les deux** :
1. `CLAUDE.md` — ajouter ou modifier la section concernée
2. `memory/` — mettre à jour le fichier `.md` correspondant + l'index `MEMORY.md`

### Aucune action sans validation explicite

Ne jamais exécuter une action sans que Maxime l'ait demandée explicitement :
- Générer ou modifier du code
- Lire des fichiers ou explorer le dépôt
- Appeler Jira ou tout outil externe
- Mettre à jour la mémoire

Une question ("pourquoi X ?") ou une phrase de reprise ("reprends sur NTR-XX") n'est pas une instruction. Répondre uniquement par une explication ou une confirmation de contexte, puis attendre.

### Pas d'initiative

Maxime code lui-même. Ne proposer que ce qui est demandé. Ne pas nettoyer, refactorer, ajouter de gestion d'erreur, ni créer de fichiers au-delà du strict périmètre demandé.

### Docs locaux avant Jira

Utiliser les fichiers locaux en priorité — ne jamais appeler Jira sans demande explicite de Maxime. Tout le backlog et toute la documentation sont disponibles localement (voir "Fichiers clés").

### Ne jamais modifier `playbook/tools/import_jira.py`

Script partagé critique — ne le toucher sous aucun prétexte sans validation explicite.

---

## Profil utilisateur

**Maxime** — développeur backend C# / ASP.NET Core. Maîtrise DDD (Aggregate Root, Value Object, Application Service), architecture 4 couches, Keycloak, EF Core. Pas besoin d'explications pédagogiques sur ces sujets.

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

## Fichiers clés — où chercher quoi

Racine de la documentation : `../docs/pages/backend/`

### Design (sources de vérité architecture)

| Fichier | Contenu |
|---|---|
| `design/design-domain.md` | Modèle domaine, agrégats, invariants |
| `design/design-application.md` | Couche Application — patterns, interfaces, DTOs |
| `design/design-infrastructure.md` | Couche Infrastructure — EF Core, Redis, Hangfire |
| `design/design-api.md` | Couche API — controllers, routing, auth JWT, **table des routes** |
| `design/Regles-metier.md` | Formules BMR/TDEE + invariants domaine |
| `design/regles-metier-consolidees.md` | **Toutes les règles métier par entité** — référence unique (évite de naviguer entre les 4 fichiers de design) |

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
| `annexes/concept-moteur-architecture.md` | Concept architectural "Moteur (Engine)" — définition, patterns Strategy + Factory, exemple `NutritionCalculator` |

### Features

| Fichier | Contenu |
|---|---|
| `features/nutrition-calculator.md` | Contrat du Moteur de calcul nutritionnel — méthodes, formules BMR, `MacroGrams`, `GetDefaultMacros` |


---

## Conventions de code

### Workflow API-first — ordre obligatoire

```
Interfaces → API layer → Application layer → Infrastructure layer
```

Avant chaque ticket API : lire la section du controller dans `design-api.md` (table des routes) — pas seulement l'interface. L'interface est un contrat technique, pas la source de vérité des routes.

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

## Workflow Git

### Stratégie de branches

```
feature/* ──squash PR──► dev ──merge commit PR──► main (release vX.Y.Z) ──► prod
```

| Transition | Type de merge |
|---|---|
| `feature/* → dev` | Squash merge |
| `dev → main` | Merge commit (jamais squash) |
| `main → prod` | Merge commit |

### Synchronisation après release

Ne jamais créer de PRs de sync (`main → dev`, `main → prod`). Utiliser uniquement :

```bash
git fetch origin
git branch -f dev  origin/dev
git branch -f prod origin/prod
git branch -f main origin/main
```

### Format de commit

```
feat(scope): description courte #NTR-XX
```

- Référence `#NTR-XX` obligatoire
- Ne jamais inclure `Co-Authored-By:` dans les commits

---

## Workflow Jira

Uniquement si Maxime le demande explicitement. Quand déclenché :

1. Passer la sous-tâche en `En cours`
2. Commiter avec `#NTR-XX`
3. Passer la sous-tâche en `Terminé`
4. Si doc externe citée → commentaire Jira avec le lien (`https://mgnetworking.github.io/docs-nutrition/`)

Instance : `maxime-ghalem.atlassian.net`

---

## État courant du projet

**Branche active :** `feature/NTR-2-application-layer`
**Epic en cours :** NTR-2 — Application Layer
**Prochaine tâche :** NTR-38 et NTR-39 — sous-tâches de NTR-9 (DietPlansService)

Tickets terminés récemment :
- NTR-37 ✅ — WeightEntry dans UserService
- NTR-8 ✅ — RgpdController + RgpdService découplé de UserService

Travail réalisé (NTR-9) :
- `DietPlansService` — 6 méthodes implémentées (`CreateAsync`, `GetUserPlansAsync`, `GetTemplatesAsync`, `UpdateAsync`, `DeleteAsync`, `LaunchAsync`)
- `SubscriptionGuard` créée et testée
- `DietPlansServiceTest` corrigé et tous les tests implémentés
- **Moteur de calcul nutritionnel** créé dans `Application/Services/Nutrition/` :
  - `BmrFormula` (enum Domain), `MacroGrams` (value object Domain)
  - `IBmrStrategy`, `MifflinStJeorStrategy`, `HarrisBenedictStrategy`
  - `NutritionCalculator`, `NutritionCalculatorFactory`
  - Tests dans `tests/NutritionApi.Application.Tests/Nutrition/`
- `regles-metier-consolidees.md` créé
- `features/nutrition-calculator.md` créé
- `annexes/concept-moteur-architecture.md` créé

Ordre des Epics :
```
NTR-79 ✅ → NTR-4 API Layer ✅ → NTR-2 Application (en cours) → NTR-3 Infrastructure
```

