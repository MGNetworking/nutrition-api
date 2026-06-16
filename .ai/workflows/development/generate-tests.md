# generate-tests <fonctionnalité>

Génère les tests couvrant une implémentation.

---

## Flux

```
1. Lire .ai/contexts/business.md    → quels docs métier lire
2. Lire .ai/contexts/quality.md     → quels docs de validation lire
3. Lire .ai/documentation-map.md    → résoudre les chemins
4. Lire les fichiers doc référencés → comprendre le QUOI (invariants, cas limites)
5. Lire .ai/project-analysis.md     → comprendre le OÙ (tests existants, conventions)
6. Scanner le code à tester
7. Générer les tests
```

---

## Stack de test

- Framework  : xUnit
- Mocks      : Moq
- Intégration: Testcontainers (vraie BDD PostgreSQL — ne pas mocker EF Core)
- Organisation : un projet de test par couche (Domain.Tests / Application.Tests / Infrastructure.Tests / Api.Tests)
- Nommage : `MethodName_Scenario_ExpectedResult`

---

## Types de tests à générer

- Cas nominaux (chemin heureux)
- Invariants métier (violations → exceptions attendues)
- Cas limites (valeurs frontières)
