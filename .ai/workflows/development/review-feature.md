# review-feature <fonctionnalité>

Vérifie la conformité d'une implémentation avec la documentation de référence.

---

## Flux

```
1. Lire .ai/contexts/business.md      → quels docs métier lire
2. Lire .ai/contexts/architecture.md  → quels docs d'implémentation lire
3. Lire .ai/contexts/quality.md       → quels docs de validation lire
4. Lire .ai/documentation-map.md      → résoudre les chemins
5. Lire les fichiers doc référencés   → référence de conformité
6. Lire .ai/project-analysis.md       → conventions attendues
7. Scanner le code de la fonctionnalité
8. Comparer : code vs documentation
```

---

## Critères de revue

- Règles métier (source : documentation)
- Patterns d'architecture (source : documentation)
- Conventions (source : `.ai/project-analysis.md`)
- Couverture de tests (framework : xUnit + Moq + Testcontainers)

---

## Boucle d'amélioration

Après la revue, ajouter dans `.ai/improvements/2026-06.md` :

```markdown
## <feature> — <date>

### Défauts détectés
- <convention manquante dans project-analysis.md>
- <règle métier absente de la documentation>
- <contexte insuffisant>

### Action recommandée
- <mettre à jour tel fichier>
- <lancer /harness:refresh-context>
```
