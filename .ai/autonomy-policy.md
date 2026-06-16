# autonomy-policy.md

Niveaux d'autonomie applicables à ce projet.

---

## Niveaux

### Niveau 1 — Sécurité / Auth / Paiement / RGPD
Validation requise à chaque étape.

Concerne :
- Tout ce qui touche Keycloak, JWT, permissions
- Stripe, abonnements, facturation
- RGPD, suppression de données utilisateur
- RgpdService, RgpdController

### Niveau 2 — Logique métier / API
Rapport validé, puis implémentation.

Concerne :
- Services applicatifs (UserService, DietPlansService, MealService…)
- Controllers REST
- Règles métier (BMR, TDEE, macros, invariants domaine)
- Moteur de calcul nutritionnel

### Niveau 3 — DTO / Tests / Refactoring
Exécution directe après rapport.

Concerne :
- DTOs Request / Response
- Tests unitaires et d'intégration
- Refactoring sans changement de comportement

---

## Règle STOP — Composant transversal

Si un composant transversal apparaît (Factory, Strategy, Guard, Event, Domain Service, Adapter) :

**STOP** — ne pas implémenter. Produire une demande d'arbitrage et attendre validation.
Si validé : exécuter `/harness:create-adr` avant de continuer.

---

## Règle — Pas d'initiative

Maxime code lui-même. Ne proposer que ce qui est demandé.
Aucune lecture de fichier, aucun appel Jira, aucune mise à jour mémoire sans demande explicite.
