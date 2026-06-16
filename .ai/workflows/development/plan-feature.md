# plan-feature <fonctionnalité>

Analyse une fonctionnalité avant tout développement.
Produit un plan validé avec niveau d'autonomie et recommandation ADR.

---

## Flux

```
1. Identifier l'intention (fonctionnalité, domaine, périmètre)
2. Lire .ai/contexts/<contexte-pertinent>.md → liste des docs à lire
3. Lire .ai/documentation-map.md             → résoudre les chemins
4. Lire chaque fichier doc référencé         → comprendre le QUOI (métier)
5. Lire .ai/project-analysis.md              → comprendre le OÙ (physique)
6. Lire .ai/autonomy-policy.md               → niveau d'autonomie applicable
7. Scanner le code existant dans les zones identifiées
8. Produire le rapport — attendre validation
```

---

## Rapport de plan

```
RAPPORT DE PLAN

Feature                 : <description>
Niveau d'autonomie      : <1 | 2 | 3>

  1 = Sécurité / Auth / Paiement / RGPD    → validation à chaque étape
  2 = Logique métier / API                 → rapport validé, puis implémentation
  3 = DTO / Tests / Refactoring            → exécution directe après rapport

ADR recommandé          : <oui | non> — <motif si oui>

COMPOSANTS EXISTANTS RÉUTILISABLES
- <fichier> → <rôle>

COMPOSANTS MANQUANTS
- <composant> → <rôle>

COMPOSANTS TRANSVERSAUX DÉTECTÉS
- Factory / Strategy / Guard / Event / Domain Service : <oui | non — justification>

IMPACTS
- Domaine        : <fichiers>
- Application    : <fichiers>
- Infrastructure : <fichiers>
- API            : <fichiers>

ORDRE D'IMPLÉMENTATION
1. <composant>
2. <composant>

RISQUES
- <risque> → <mitigation>
```

Attendre validation avant tout code.

---

## Règle STOP — Composant transversal détecté

Si un composant transversal apparaît (Factory, Strategy, Guard, Event, Domain Service, Adapter) :

**STOP** — ne pas inclure dans ce plan. Produire :

```
DEMANDE D'ARBITRAGE

Composant proposé : <nom>
Motif             : <pourquoi nécessaire>
Alternatives      : <autres approches>
Impact            : <couches et fichiers concernés>
```

Attendre validation. Si validé : exécuter `/harness:create-adr` avant de continuer.

---

## Ce que cette commande ne fait jamais

- Générer du code
- Modifier des fichiers existants
