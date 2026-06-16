# documentation-map.md

Index de navigation vers toute la documentation du projet.
Utilisé par les workflows pour résoudre les chemins documentaires.

---

## documentation-root

```
../docs/pages/backend/
../docs/pages/backend/design/
../docs/pages/backend/features/
../docs/pages/backend/annexes/
../docs/pages/backend/livrable/
```

---

## business

Règles métier, invariants, cas d'usage, abonnements.

```
files:
  - design/Regles-metier.md
  - design/regles-metier-consolidees.md
  - 1.nutrition-introduction.md
  - 2.nutrition-cas-usage.md
  - 3.nutrition-specifications-fonctionnelles.md
  - 6.nutrition-abonnements.md
  - 7.nutrition-admin.md
  - features/bilan-nutritionnel.md
  - features/diet-plan.md
  - features/diet.md
  - features/repas.md
  - features/aliments.md
  - features/suivi-poids.md
  - features/profil-utilisateur.md
  - features/nutrition-calculator.md
  - features/rgpd.md
  - features/abonnements.md
  - features/admin-dashboard.md
  - features/authentification.md
```

---

## architecture

Modèle domaine, couches, patterns, conventions d'implémentation.

```
files:
  - design/design-domain.md
  - design/design-application.md
  - design/design-infrastructure.md
  - design/design-api.md
  - design/design-api-infrastructure.md
  - design/design-tests.md
  - annexes/Diagramme-classes.md
  - annexes/concept-moteur-architecture.md
  - annexes/workflows.md
```

---

## product

Spécifications techniques, contraintes, écrans frontend, backlog.

```
files:
  - 4.nutrition-specifications-techniques.md
  - 5.nutrition-contraintes.md
  - livrable/specs-frontend.md
  - livrable/checklist-implementation.md
  - features/openapi.md
  - features/exception-filter.md
  - features/tests-integration.md
```

---

## integrations

Services externes : Keycloak, Open Food Facts, Stripe, Hangfire.

```
files:
  - annexes/infrastructure-keycloak-admin.md
  - annexes/infrastructure-import-off.md
  - annexes/infrastructure-stripe.md
  - annexes/infrastructure-hangfire.md
```

---

## infrastructure

Setup technique, déploiement, configuration, jobs.

```
files:
  - annexes/infrastructure-setup.md
  - annexes/infrastructure-docs.md
```

---

## quality

Tests, TDD, couverture, intégration continue.

```
files:
  - design/design-tests.md
  - features/tests-integration.md
```
