# Pièges connus

## Architecture

### Lire design-api.md avant tout controller
La table des routes dans `design-api.md` est la source de vérité des routes HTTP.
L'interface `IXxxService` est un contrat technique, pas une source de vérité des routes.
Ne pas implémenter un controller sans avoir lu la section correspondante dans `design-api.md`.

### Workflow API-first obligatoire
Ordre : Interfaces → API layer → Application layer → Infrastructure layer.
Implémenter dans un autre ordre crée des incohérences de contrat.

### Composants transversaux — règle STOP
Factory, Strategy, Guard, Event, Domain Service, Adapter → demande d'arbitrage obligatoire.
Ne jamais les intégrer directement dans un plan de feature sans validation explicite.

## Tests

### MacroDistributionDto — int, pas float
Les grammes de macros sont des `int` dans `MacroDistributionDto`.
Écrire `Assert.Equal(150, dto.Proteins)` et non `Assert.Equal(150.0f, dto.Proteins)`.

### Ne pas mocker les repositories EF Core Testcontainers
Les tests d'intégration utilisent Testcontainers (vraie BDD) — ne pas substituer par des mocks en mémoire.

## Git

### Pas de PRs de sync main → dev
Utiliser `git branch -f` après `git fetch` — les PRs de sync créent une divergence infinie.

### Co-Authored-By interdit
Ne jamais ajouter `Co-Authored-By: Claude` dans les messages de commit.
