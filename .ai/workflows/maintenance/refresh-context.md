# refresh-context

Resynchronise `.ai/` avec l'état courant de la documentation du projet.
À utiliser quand la documentation a évolué depuis le dernier bootstrap.

> Ne met pas à jour `.ai/workflows/` — utiliser `/harness:sync-harness` pour ça.

---

## Étape 1 — Détecter les dérives documentaires

Lire `.ai/documentation-map.md` → collecter toutes les racines et fichiers référencés.
Explorer les racines déclarées dans `documentation-root`.

Détecter :
- Nouveaux fichiers documentaires non référencés
- Fichiers supprimés encore référencés
- Nouvelles racines documentaires non déclarées
- Catégories ayant significativement évolué

---

## Étape 2 — Rapport de dérive

```
DOCUMENTATION-MAP
  Nouvelles racines potentielles       : <liste>
  Nouveaux fichiers non référencés     : <liste>
  Fichiers supprimés encore référencés : <liste>
  Catégories à mettre à jour           : <liste>

CONTEXTES POTENTIELLEMENT OBSOLÈTES
  - <contexte> : <raison>

STRUCTURE PHYSIQUE
  Changements détectés : <oui | non>
```

Si aucune dérive : indiquer que `.ai/` est à jour et s'arrêter.

Attendre validation.

---

## Étape 3 — Mise à jour

Appliquer les corrections validées :
- Mettre à jour `.ai/documentation-map.md`
- Mettre à jour les contextes concernés dans `.ai/contexts/`
- Si structure physique changée : mettre à jour `.ai/project-analysis.md`

---

## Ce que cette commande ne fait jamais

- Modifier la documentation du projet
- Modifier le fichier agent (CLAUDE.md, AGENTS.md...)
- Supprimer `.ai/decisions/`, `.ai/knowledge/` ou `.ai/improvements/`
- Modifier `.ai/workflows/` (→ `/harness:sync-harness`)
