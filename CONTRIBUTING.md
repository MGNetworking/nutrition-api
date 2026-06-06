# Contribution & Gestion des branches

## Stratégie de branches

```
feature/* ──squash PR──► dev ──PR──► prod (déploiement VPS)
                          │
             PR milestone──► main (release taguée vX.Y.Z)
```

### Rôle de chaque branche

| Branche | Rôle | Alimentée par | Déploiement |
|---------|------|---------------|-------------|
| `feature/*` | Développement isolé d'une feature / Epic | — | — |
| `dev` | Intégration — état courant du travail | `feature/*` via squash PR | — |
| `prod` | Production — VPS | `dev` via PR | Automatique au merge |
| `main` | Releases stables taguées | `dev` via PR milestone | Tag `vX.Y.Z` + CHANGELOG |

### Règles de protection

Toutes les branches protégées appliquent :

- PR obligatoire avant tout merge (aucun push direct)
- CI `build-and-test` verte obligatoire
- Branche à jour avec la cible avant merge
- Force push et suppression interdits

`main` et `prod` ont en plus `enforce_admins` activé — même l'admin du dépôt doit respecter les règles.

---

## Stratégie CI par transition

Les tests sont différenciés selon la transition pour éviter de rejouer inutilement les mêmes vérifications.

| PR | Workflow | Ce qui s'exécute |
|----|----------|-----------------|
| `feature/* → dev` | `ci-unit.yml` | Build + tests unitaires |
| `dev → prod` | `ci-deploy.yml` | Build Release + déploiement VPS |
| `dev → main` | `ci-release.yml` | Build + tests unitaires + couverture + rapport PR |

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
- Avoir la CI verte (`build-and-test`)
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

# Synchroniser dev localement
git checkout dev && git pull origin dev

# Créer une branche feature
git checkout -b feature/<nom>

# Pousser et suivre la branche distante
git push -u origin feature/<nom>
```
