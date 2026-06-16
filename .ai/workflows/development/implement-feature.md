# implement-feature <fonctionnalité>

Implémente une fonctionnalité après validation du plan.
Prérequis : `/harness:plan-feature` validé.

---

## Flux

```
1. Lire .ai/contexts/business.md       → quels docs métier lire
2. Lire .ai/contexts/architecture.md   → quels docs d'implémentation lire
3. Lire .ai/documentation-map.md       → résoudre les chemins documentaires
4. Lire les fichiers doc référencés    → comprendre le QUOI (métier + patterns)
5. Lire .ai/project-analysis.md        → comprendre le OÙ (couches, conventions)
6. Lire .ai/autonomy-policy.md         → niveau d'autonomie applicable
7. Scanner le code existant
8. Produire le rapport — attendre validation si niveau 1 ou 2
9. Implémenter
```

---

## Rapport préalable

```
RAPPORT D'IMPLÉMENTATION

Feature                    : <description>
Niveau d'autonomie         : <1 | 2 | 3>
Code existant réutilisable : <liste>
Composants manquants       : <liste>
Impacts architecture       : <couches concernées>
Ordre recommandé           : <séquence>
Risques                    : <liste>
```

---

## Règle STOP — Composant transversal

Si un composant transversal apparaît (Factory, Strategy, Guard, Event, Domain Service, Adapter) :

**STOP** — produire :

```
DEMANDE D'ARBITRAGE

Composant proposé : <nom>
Motif             : <pourquoi nécessaire>
Alternatives      : <autres approches>
Impact            : <couches et fichiers concernés>
```

Attendre validation. Si validé : exécuter `/harness:create-adr` avant d'implémenter.

---

## Règles

- Respecter les conventions de `.ai/project-analysis.md`
- Ne jamais créer de fichier hors du périmètre validé
- Stack : C# 13 / ASP.NET Core 10 / EF Core 10 + Npgsql / PostgreSQL
- Framework de test : xUnit + Moq + Testcontainers
