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

---

## Profil utilisateur

**Maxime** — développeur backend C# / ASP.NET Core. Maîtrise DDD (Aggregate Root, Value Object, Application Service), architecture 4 couches, Keycloak, EF Core. Pas besoin d'explications pédagogiques sur ces sujets.

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
| `annexes/concept-moteur-architecture.md` | Concept "Moteur (Engine)" — patterns Strategy + Factory |
| `features/nutrition-calculator.md` | Contrat du moteur de calcul nutritionnel |

---

## Rules et conventions

| Fichier | Sujet |
|---|---|
| `.claude/rules/tdd.md` | TDD — Red/Green/Refactor, ordre fichiers, couverture attendue |
| `.claude/rules/xml-documentation.md` | Documentation XML C# — interfaces, services, repositories, entités |
| `.claude/conventions.md` | Stack technique + conventions de code (patterns, DTO, exceptions) |
| `.claude/jira-workflow.md` | Workflow Jira — transitions, commits, commentaires |
| `CONTRIBUTING.md` | Workflow Git — branches, stratégie de merge, format de commit |

---

## État courant du projet

→ Voir `memory/project_state.md`

**Branche active :** `feature/NTR-2-application-layer`

### Avancement Application Layer (NTR-2)

| Ticket | Sujet | État |
|---|---|---|
| NTR-8 | UserService + RgpdService | ✅ |
| NTR-36 | Créer/mettre à jour profil utilisateur | ✅ |
| NTR-37 | Gestion des pesées (WeightEntry) | ✅ |
| NTR-113 | Méthodes RGPD Domain + Application | ✅ |
| NTR-114 | Export RGPD — DTO + ZIP controller | ✅ |
| NTR-9 | DietPlansService | ✅ |
| NTR-116 | Moteur de calcul nutritionnel | ✅ |
| NTR-38 | CRUD plans personnels (DietPlan) | ✅ |
| NTR-39 | Lister les templates partagés | ✅ |
| NTR-40 | Lancer un DietPlan (LaunchAsync) | ✅ |
| NTR-10 | Cycle de vie d'un régime (Diet) | ✅ |
| NTR-11 | MealService | ✅ |
| NTR-47 | SubscriptionGuard | ✅ |
| NTR-95 | Contrôle tier DietPlan | ✅ |
| NTR-96 | Restriction templates selon tier | ✅ |
| NTR-12 | FoodItemService | 🔲 prochain |
| NTR-13 | AdminService | 🔲 |
| NTR-15 | NutritionService | 🔲 |
