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

### Annoncer tous les fichiers touchés — avant d'écrire

Avant d'écrire quoi que ce soit, donner la **liste complète** des fichiers créés ou modifiés, et attendre l'accord.

Cela inclut les fichiers **collatéraux** qu'une règle du projet impose de mettre à jour : la `nav:` de `mkdocs.yml`, un `index.md`, `MEMORY.md`, le sommaire d'un README.

Une règle qui dit *ce qu'il faut faire* n'autorise pas à *le faire seul*. Mentionner un fichier au détour d'une option ou d'une justification ne vaut pas validation — il doit figurer dans une liste explicite.

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
| Organisation de la documentation | Classement **par système** : `systemes/`, `briques/`, `qualite/`, `reference/`. `annexes/` et `features/` supprimés, `design/` inchangé. Le public (utilisateur/interne) ne classe plus. Voir `.claude/rules/documentation-projet.md`. | 2026-07-26 |
| Postman / Newman en CI | **Écarté** comme moteur de tests : incapable de vérifier l'état en base, l'expiration du TTL Redis, ou de produire de la couverture. La collection Postman reste un outil de développement. | 2026-07-22 |

### Pas de fichier de passation de session

Les passations (`.claude/sessions/handoff-*.md`) ne sont **plus utilisées** — supprimées le 2026-07-26.
Elles se périmaient en quelques jours tout en ayant l'apparence d'une source de vérité. La reprise de
contexte s'appuie sur : ce fichier (règles et décisions), `memory/` (contexte projet), Jira
(avancement), la documentation publiée (fonctionnement).

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

### Systèmes — un dossier par système, tout ce qui lui appartient

| Fichier | Contenu |
|---|---|
| `systemes/index.md` | Catalogue des systèmes — la porte d'entrée |
| `systemes/<systeme>/index.md` | Le besoin et le périmètre (diet, repas, aliments, rgpd…) |
| `systemes/<systeme>/workflow-*.md` | Le fonctionnement de bout en bout |
| `systemes/aliments/` | Recherche, cache, mise à disposition, source Open Food Facts |
| `systemes/bilan-nutritionnel/moteur-de-calcul.md` | Contrat du moteur de calcul nutritionnel |

Le public (utilisateur / interne) est porté par le champ `**Type :**` de chaque fiche, **pas** par
un dossier.

### Briques techniques, qualité, référence

| Fichier | Contenu |
|---|---|
| `briques/` | Technologies tierces — `redis.md`, `hangfire.md`, `keycloak-admin.md`, `stripe.md`, `environnement-local.md` |
| `qualite/` | `niveaux-de-tests.md`, `recensement-des-tests.md` |
| `reference/diagramme-classes.md` | Diagramme Mermaid du modèle domaine |
| `reference/concept-moteur.md` | Concept "Moteur (Engine)" — patterns Strategy + Factory |

---

## Rules et conventions

| Fichier | Sujet |
|---|---|
| `.claude/rules/tdd.md` | TDD — Red/Green/Refactor, ordre fichiers, couverture attendue |
| `.claude/rules/xml-documentation.md` | Documentation XML C# — interfaces, services, repositories, entités |
| `.claude/rules/documentation-projet.md` | Documentation Markdown — 3 niveaux (feature / workflow / référence), gabarits |
| `.claude/rules/conventions.md` | Conventions de code (workflow API-first, patterns, DTO, exceptions) |
| `.claude/rules/jira-workflow.md` | Workflow Jira — transitions, commits, commentaires |
| `CONTRIBUTING.md` | Workflow Git — branches, stratégie de merge, format de commit |

---

## Suivi du projet

L'état d'avancement n'est **pas** suivi dans ce fichier — la source de vérité est Jira :

- **Backlog et statuts :** https://maxime-ghalem.atlassian.net/ — projet `NTR`
- **Accès :** MCP `atlassian` si chargé, sinon API REST (voir `memory/reference_jira.md`)
- Ne consulter Jira que sur demande explicite de Maxime (règle "Docs locaux avant Jira")
