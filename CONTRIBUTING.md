# Contribution & Gestion des branches

## Stratégie de branches

```
feature/* ──squash PR──► dev ──merge commit PR──► main (release taguée vX.Y.Z)
                                                   │
                                                   └──PR──► prod (déploiement VPS)
```

### Rôle de chaque branche

| Branche | Rôle | Alimentée par | Déploiement |
|---------|------|---------------|-------------|
| `feature/*` | Développement isolé d'une feature / Epic | — | — |
| `dev` | Intégration — état courant du travail | `feature/*` via squash PR | — |
| `prod` | Production — VPS | `main` via PR | Automatique au merge |
| `main` | Releases stables taguées | `dev` via PR milestone | Tag `vX.Y.Z` + CHANGELOG |

### Règles de protection

Toutes les branches protégées appliquent :

- PR obligatoire avant tout merge (aucun push direct)
- CI verte obligatoire (check requis selon la branche cible)
- Branche à jour avec la cible avant merge
- Force push et suppression interdits

`enforce_admins` est désactivé — l'admin du dépôt peut bypasser les règles si nécessaire (ex: PRs de sync ou Release Please).

---

## Stratégie CI par transition

Les tests sont différenciés selon la transition pour éviter de rejouer inutilement les mêmes vérifications.

| PR | Workflow | Ce qui s'exécute |
|----|----------|-----------------|
| `feature/* → dev` | `ci-unit.yml` | Build + tests unitaires |
| `main → dev` (sync) | `ci-unit.yml` | **ignoré** (`github.head_ref != 'main'`) |
| `dev → prod` | `ci-deploy.yml` | Build Release + déploiement VPS |
| `main → prod` (sync) | `ci-deploy.yml` | **ignoré** (`github.head_ref != 'main'`) |
| `dev → main` | `ci-release.yml` | Build + tests unitaires + couverture + rapport PR |
| Release Please PR | `ci-release.yml` | **ignoré** (`github.actor != 'github-actions[bot]'`) |

> Les PRs de synchronisation `main → dev` et `main → prod` ne déclenchent pas le CI — le code vient de `main` qui est déjà testé. Les PRs automatiques de Release Please sont également ignorées pour éviter les boucles.

### Smoke tests — feuille de route

Les smoke tests vérifient que **le système déployé est vivant et cohérent**, pas la logique métier.
Ils s'exécuteront dans `ci-deploy.yml` après le déploiement VPS, en 3 niveaux progressifs.

#### Niveau 1 — À implémenter avec l'API layer (NTR-4)

Prérequis : endpoint `/health` disponible dans l'API.

```
GET /health                    → 200 OK
GET /swagger/v1/swagger.json   → 200 OK  (OpenAPI accessible)
GET /api/users/xxx             → 401 Unauthorized  (auth middleware actif, pas 500)
```

Pas de base de données requise — vérifie uniquement que l'app démarre et répond.

#### Niveau 2 — À implémenter avec la couche Infrastructure (NTR-3) + VPS loué

Prérequis : PostgreSQL, Redis et Keycloak déployés sur le VPS.

```
GET /health/ready              → 200 OK  (connectivité PostgreSQL + Redis confirmée)
POST /api/diets                → 401 Unauthorized  (Keycloak répond, pas 500)
```

#### Niveau 3 — À implémenter avant v1.0.0 (MVP)

Prérequis : token de test dédié configuré dans les secrets GitHub (`TEST_JWT_TOKEN`).

Scénario end-to-end léger :
```
1. POST /api/users/{id}/profile    → 200  (créer un profil)
2. POST /api/diet-plans            → 201  (créer un DietPlan)
3. POST /api/diets                 → 201  (lancer une Diet)
4. GET  /api/diets/{id}/bilan      → 200  (bilan sans erreur 5xx)
```

---

## Environnement de développement local

L'API tourne sur la machine hôte ; PostgreSQL, Redis et Keycloak tournent dans des conteneurs.

### Prérequis

- Docker Desktop démarré
- SDK .NET et l'outil EF Core : `dotnet tool install --global dotnet-ef`

### Démarrer

```bash
./scripts/dev-up.sh
```

Le script démarre les trois services, attend qu'ils soient prêts, applique les migrations EF Core
et charge les données de test. Il ne lance pas l'API : celle-ci se démarre depuis l'IDE avec le
débogueur, ou par `dotnet run --project src/NutritionApi.Api`.

Le script est idempotent — le relancer sur un environnement déjà démarré ne pose aucun problème.

### Arrêter et réinitialiser

```bash
./scripts/dev-down.sh              # arrête les services, conserve les données
./scripts/dev-down.sh --volumes    # arrête les services et supprime les données
./scripts/dev-reset.sh             # repart d'une base vierge (migrations + seed rejoués)
```

### Services et comptes

| Service | Adresse |
|---|---|
| PostgreSQL | `localhost:5445` — base `nutrition_dev`, `postgres` / `postgres` |
| Redis | `localhost:6336` |
| Keycloak | `http://localhost:8778` — console admin `admin` / `admin` |
| API | `http://localhost:5099` — Swagger sur `/swagger` |

Comptes de test provisionnés automatiquement, mot de passe `test` :

| Compte | Rôle Keycloak | Abonnement |
|---|---|---|
| `test-user` | `user` | Free |
| `test-pro` | `user` | Pro |
| `test-admin` | `admin` | Free |

Obtenir un jeton :

```bash
curl -X POST "http://localhost:8778/realms/nutrition/protocol/openid-connect/token" \
  -d "client_id=nutrition-api" -d "grant_type=password" \
  -d "username=test-user" -d "password=test"
```

> Les ports externes sont volontairement décalés (5445, 6336, 8778) pour ne pas entrer en conflit
> avec des instances déjà présentes sur le poste. Les ports internes aux conteneurs restent standards.

### Mode Docker — API conteneurisée

Pour valider l'image de l'API ou reproduire un fonctionnement proche de la production, l'API peut
tourner dans un conteneur aux côtés de ses services :

```bash
./scripts/docker-up.sh              # build si nécessaire, puis démarre la stack complète
./scripts/docker-up.sh --rebuild    # force la reconstruction des images
./scripts/dev-down.sh               # arrête l'ensemble
```

L'API est alors exposée sur `http://localhost:5100` — port distinct de 5099 pour que les deux modes
puissent coexister. Elle charge `appsettings.Docker.json` et joint les services par leur nom
(`postgres`, `redis`, `keycloak`).

Les services `api` et `migrations` sont déclarés sous le profil Compose `full` : `docker compose up -d`
continue de ne démarrer que les trois services d'infrastructure.

**Migrations.** Un conteneur one-shot les applique puis s'arrête ; l'API ne démarre qu'après sa réussite
(`depends_on` / `service_completed_successfully`). L'API n'applique jamais les migrations elle-même :
la base n'évolue qu'au déploiement. Ce schéma préfigure le Job Kubernetes de production.

```
postgres healthy  →  migrations (exit 0)  →  api
```

---

## Workflow feature → dev

### 1. Créer la branche feature depuis `dev`

```bash
git checkout dev
git pull origin dev
git checkout -b feature/<nom-du-ticket>
# Exemple : feature/api-layer, feature/infrastructure-layer
```

### 2. Développer et commiter

Format de commit obligatoire (commits conventionnels) :

```
<type>(<scope>): <description courte> #NTR-XX

feat(api): UsersController profil et pesées #NTR-22
fix(domain): invariant Meal corrigé #NTR-66
test(application): tests UserService #NTR-36
chore(ci): mise à jour workflow GitHub Actions
```

Types autorisés : `feat`, `fix`, `refactor`, `test`, `chore`, `docs`, `perf`

### 3. Pousser et ouvrir une PR vers `dev`

```bash
git push origin feature/<nom>
# Puis ouvrir la PR sur GitHub : feature/<nom> → dev
```

La PR doit :
- Référencer le ticket Jira (`#NTR-XX` dans le titre ou la description)
- Avoir la CI verte (`CI — Tests unitaires / Build & Tests unitaires`)
- Être mergée en **squash merge** (1 commit propre par PR)

---

## Workflow dev → prod (déploiement)

Ouvrir une PR `dev → prod` une fois les features validées sur `dev`.

Le merge déclenche `ci-deploy.yml` :
1. Build Release (validation)
2. Déploiement automatique sur le VPS *(activé quand le VPS sera loué)*
3. Smoke tests post-déploiement *(activés progressivement — voir feuille de route ci-dessus)*

---

## Workflow dev → main (release)

Les merges vers `main` correspondent à des **milestones produit** (fin d'une ou plusieurs Epics).

La PR `dev → main` doit être mergée avec **"Create a merge commit"** (pas squash) pour préserver l'historique et éviter la divergence entre `dev` et `main`.

Versionnage sémantique :

| Milestone | Version |
|-----------|---------|
| Domain + Application (NTR-1 + NTR-2) | `v0.1.0` |
| API layer (NTR-4) | `v0.2.0` |
| Infrastructure layer (NTR-3) | `v0.3.0` |
| MVP complet | `v1.0.0` |

Le CHANGELOG est généré automatiquement par [Release Please](https://github.com/googleapis/release-please) à partir des commits conventionnels.

---

## Nommage des branches

| Contexte | Format | Exemple |
|----------|--------|---------|
| Epic / feature | `feature/<nom>` | `feature/api-layer` |
| Correctif urgent | `fix/<nom>` | `fix/diet-invariant` |
| Maintenance CI/CD | `chore/<nom>` | `chore/update-actions` |

---

## Commandes utiles

```bash
# Voir l'état de toutes les branches
git branch -a

# Mettre à jour les branches locales (option 1 — avec checkout)
git checkout main && git pull origin main
git checkout dev  && git pull origin dev
git checkout prod && git pull origin prod

# Mettre à jour les branches locales (option 2 — sans changer de branche)
git fetch origin
git branch -f main origin/main   # déplace le pointeur local main sur origin/main
git branch -f dev  origin/dev
git branch -f prod origin/prod

# Créer une branche feature
git checkout dev
git checkout -b feature/<nom>

# Pousser et suivre la branche distante
git push -u origin feature/<nom>
```

## Collection Postman

Une collection de développement est versionnée dans `postman/` :

| Fichier | Contenu |
|---|---|
| `nutrition-api.postman_collection.json` | Les 36 endpoints, groupés par ressource |
| `nutrition-dev.postman_environment.json` | Mode dev — API sur le host, port 5099 |
| `nutrition-docker.postman_environment.json` | Mode Docker — API conteneurisée, port 5100 |

### Utilisation

1. Importer la collection et les deux environnements dans Postman
2. **Sélectionner l'environnement correspondant à la façon dont l'API tourne** — « Dev local » après
   un `dotnet run`, « Docker » après un `./scripts/docker-up.sh`. Se tromper d'environnement donne des
   requêtes qui n'aboutissent pas.
3. Exécuter une requête du dossier **Authentification** — le jeton est enregistré automatiquement
   et appliqué à toutes les requêtes suivantes

> **Les dossiers Users et Admin renvoient actuellement 500.** Leurs controllers injectent respectivement
> `IFoodItemService` (NTR-54) et `IAdminService` (NTR-55), non enregistrés tant que leurs dépendances
> Infrastructure n'existent pas. Le controller entier devient inconstructible — y compris pour les
> endpoints sans rapport avec ces services, comme la lecture du profil ou l'historique des pesées.
> Les dossiers **Diet Plans**, **Diets**, **Meals** et **Nutrition** sont pleinement fonctionnels.

Trois requêtes de jeton sont fournies, une par compte de test, pour basculer d'un profil à l'autre
(Free, Pro, admin) sans manipulation.

> **Les enums sont sérialisés en nombres, pas en chaînes** — aucun `JsonStringEnumConverter` n'est
> configuré. Exemple : `"gender": 1` (Male), `"mealType": 2` (Lunch). Les correspondances sont
> documentées dans la description de chaque requête.

Les identifiants par défaut de l'environnement correspondent aux données de `seed-dev.sql` : les
requêtes fonctionnent sans réglage préalable après un `./scripts/dev-up.sh`.

> Périmètre : outil de développement et de documentation. Cette collection n'est **pas** exécutée en
> CI — les tests automatisés sont en xUnit (voir `docs/pages/backend/features/interne/tests-integration.md`).
