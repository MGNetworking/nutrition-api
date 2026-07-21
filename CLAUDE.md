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

## Décisions d'architecture

Arbitrages actés — ne pas les remettre en cause sans demande explicite de Maxime.

| Sujet | Décision | Date |
|---|---|---|
| Tests d'intégration externe (niveau 3) | **docker-compose** avec les 3 services (PostgreSQL, Redis, **Keycloak**) — réutilisé en CI. Testcontainers écarté. | 2026-07-21 |
| Déploiement production | **VPS** (le *où*) + **K3s** (l'orchestrateur) — les manifests restent portables vers un cloud managé. | 2026-07-21 |

**Trois environnements distincts**, sans obligation d'alignement entre eux :

| Environnement | Orchestrateur |
|---|---|
| Dev local | docker-compose |
| CI — tests niveau 3 | docker-compose |
| Production | K3s |

Seules les **versions d'images** doivent rester alignées entre le docker-compose et les manifests K3s.

> Les mentions « Kubernetes » dans `CONFIGURATION.md` et les docs de design sont **correctes** (K3s = Kubernetes allégé) — ne pas les retirer.

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
| `.claude/rules/conventions.md` | Conventions de code (workflow API-first, patterns, DTO, exceptions) |
| `.claude/rules/jira-workflow.md` | Workflow Jira — transitions, commits, commentaires |
| `CONTRIBUTING.md` | Workflow Git — branches, stratégie de merge, format de commit |

---

## Suivi du projet

L'état d'avancement n'est **pas** suivi dans ce fichier — la source de vérité est Jira :

- **Backlog et statuts :** https://maxime-ghalem.atlassian.net/ — projet `NTR`
- **Accès :** MCP `atlassian` si chargé, sinon API REST (voir `memory/reference_jira.md`)
- Ne consulter Jira que sur demande explicite de Maxime (règle "Docs locaux avant Jira")
