# sync-harness

Synchronise `.ai/workflows/` avec la dernière version du Harness.
À utiliser après une mise à jour du projet Harness.

> Prérequis : `.ai/` doit exister.

---

## Étape 1 — Identifier la source Harness

Détecter où le Harness est installé :
- `~/.claude/harness/` (via install Claude adapter)
- Chemin défini dans `.ai/user/preferences.md` (clé `harness.path`)
- Chemin passé en argument

---

## Étape 2 — Détecter les changements

Comparer `.ai/workflows/` avec la version source du Harness.

Détecter :
- Workflows modifiés depuis le dernier sync
- Nouveaux workflows
- Workflows supprimés

---

## Étape 3 — Rapport de delta

```
WORKFLOWS MODIFIÉS
- <catégorie>/<fichier> → <nature du changement>

NOUVEAUX WORKFLOWS
- <catégorie>/<fichier>

WORKFLOWS SUPPRIMÉS
- <catégorie>/<fichier>
```

Si aucun delta : indiquer que les workflows sont à jour et s'arrêter.

Attendre validation.

---

## Étape 4 — Synchronisation

Appliquer les changements validés.
Mettre à jour les adaptateurs agents si nécessaire.

---

## Ce que cette commande ne fait jamais

- Modifier `.ai/contexts/`, `.ai/documentation-map.md`, `.ai/project-analysis.md`
- Toucher la documentation du projet
- Supprimer `.ai/decisions/`, `.ai/knowledge/` ou `.ai/improvements/`
