# project-onboarding

Permet à un nouvel agent ou un nouveau développeur de comprendre rapidement le projet.

---

## Flux

```
1. Lire .ai/HARNESS.md                    ← comprendre la structure
2. Lire .ai/documentation-map.md          ← localiser la documentation
3. Lire .ai/contexts/product.md           ← quels docs produit lire
4. Lire .ai/contexts/business.md          ← quels docs métier lire
5. Lire .ai/contexts/architecture.md      ← quels docs architecture lire
6. Lire les fichiers doc prioritaires     ← comprendre le QUOI
7. Lire .ai/project-analysis.md           ← comprendre le OÙ
8. Lire .ai/autonomy-policy.md            ← connaître les niveaux d'autonomie
9. Lire .ai/decisions/README.md           ← décisions majeures
10. Lire .ai/knowledge/lessons-learned.md ← retours d'expérience
11. Produire le rapport d'onboarding
```

---

## Rapport d'onboarding

```
RAPPORT D'ONBOARDING — nutrition-api

VISION PRODUIT
  Que fait ce produit ?
  Pour qui ?
  Périmètre courant :

PRINCIPAUX CONCEPTS MÉTIER
  - <concept> : <définition courte>

ARCHITECTURE
  Type            : DDD 4 couches
  Stack           : C# 13 / ASP.NET Core 10 / EF Core 10 / PostgreSQL
  Couches         : Domain / Application / Infrastructure / Api
  Conventions     : <résumé depuis project-analysis.md>

INTÉGRATIONS
  - Keycloak : auth OAuth2/OIDC
  - Open Food Facts : import aliments quotidien (Hangfire)
  - Stripe : paiements et abonnements
  - Hangfire : jobs planifiés

RISQUES ET PIÈGES CONNUS
  - <risque> (source : .ai/knowledge/known-pitfalls.md)

ADR IMPORTANTES
  - <source : .ai/decisions/README.md>

PROCHAINE ÉTAPE
  → <source : CLAUDE.md section "État courant du projet">

POUR COMMENCER
  1. Lire .ai/autonomy-policy.md
  2. Exécuter /harness:plan-feature <feature>
```

---

## Ce que cette commande ne fait jamais

- Modifier des fichiers
- Implémenter quoi que ce soit
