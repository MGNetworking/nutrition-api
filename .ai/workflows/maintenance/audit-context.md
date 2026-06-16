# audit-context [--strict]

Vérifie la cohérence et la qualité du Harness `.ai/`.
Produit un score /100 avec erreurs et avertissements distincts.

---

## Vérifications

### V1 — Liens documentaires
Chaque chemin dans `files` existe dans l'une des racines de `documentation-root`.

### V2 — Format documentation-map
`documentation-root` présent avec au moins une valeur.
6 catégories standard vérifiées : business, architecture, product, integrations, infrastructure, quality.
Aucun chemin de code ni nom de service/repository/controller.

### V3 — Contextes purs
Chaque contexte ne contient que des pointeurs vers `documentation-map.md`.
Aucun contenu métier ou technique direct.

### V4 — Flux des workflows
Chaque workflow lit les contextes avant `project-analysis.md`.
Ordre : contextes → documentation-map → documentation (QUOI) → project-analysis (OÙ) → autonomy-policy.

### V5 — Couverture documentaire
Chaque fichier dans les racines documentaires est référencé dans au moins une catégorie.

### V6 — Cohérence globale
Références orphelines et contextes non référencés par aucun workflow.

### V7 — Fichiers obligatoires
```
.ai/HARNESS.md
.ai/documentation-map.md
.ai/project-analysis.md
.ai/autonomy-policy.md
.ai/decisions/README.md
.ai/knowledge/glossary.md
.ai/knowledge/lessons-learned.md
.ai/knowledge/known-pitfalls.md
.ai/improvements/         (≥ 1 fichier)
.ai/contexts/             (≥ 1 fichier)
.ai/workflows/bootstrap/
.ai/workflows/development/
.ai/workflows/maintenance/
```

### V8 — Maintenance présente
`refresh-context.md`, `project-onboarding.md`, `sync-harness.md` existent.

### V9 — Universalité
Aucun terme DDD dans les noms de fichiers `contexts/` et `workflows/`.
Termes interdits : entity, repository, aggregate, value-object, domain.

### V10 — Doublons
Aucun fichier documentaire référencé dans deux catégories différentes.

### V11 — Catégories vides
Aucune catégorie sans fichiers associés.

### V12 — Config locale
`.ai/user/` dans `.gitignore`. Aucun `CLAUDE.workflow.md` présent.

### V13 — Fichier agent adaptateur
Le fichier agent (CLAUDE.md, AGENTS.md...) contient une référence vers `.ai/HARNESS.md`.

---

## Rapport de sortie

```
AUDIT HARNESS — <date>

Score : XX/100

ERREURS (bloquantes)
[V1] <description>

AVERTISSEMENTS
[V5] <description>

RECOMMANDATIONS
- <action corrective>
```

En mode `--strict` : tout avertissement devient une erreur.

---

## Barème

| V | Libellé | Points |
|---|---|---|
| V1 | Liens valides | 12 |
| V2 | Format map | 8 |
| V3 | Contextes purs | 12 |
| V4 | Flux workflows | 8 |
| V5 | Couverture docs | 12 |
| V6 | Cohérence globale | 8 |
| V7 | Fichiers obligatoires | 10 |
| V8 | Maintenance présente | 5 |
| V9 | Universalité | 5 |
| V10 | Sans doublons | 3 |
| V11 | Sans catégories vides | 3 |
| V12 | Config locale | 7 |
| V13 | Fichier agent | 7 |
