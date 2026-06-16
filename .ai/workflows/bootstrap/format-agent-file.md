# format-agent-file

Transforme le fichier d'intégration de l'agent (CLAUDE.md, AGENTS.md, GEMINI.md...)
en adaptateur pur pointant vers `.ai/HARNESS.md`.

---

## Étape 1 — Identifier le fichier agent

Détecter quel fichier d'intégration agent existe à la racine du projet :
- `CLAUDE.md` (Claude Code)
- `AGENTS.md` (OpenAI Codex)
- `GEMINI.md` (Gemini)
- `.roo/rules.md` (Roo Code)
- Autre fichier de configuration agent

Lire ce fichier dans son intégralité.

---

## Étape 2 — Inventaire

Pour chaque bloc du fichier, déterminer sa nature :

| Type | Critère | Destination |
|---|---|---|
| **Pointeur Harness** | Référence vers `.ai/` | Conserver si correct |
| **Contenu projet** | Specs, design, invariants, routes | → `docs/` (si pas documenté) |
| **Config personnelle** | Profil, préférences, règles de collaboration | → `.ai/user/` (gitignored) |
| **Temporaire** | WIP, ticket en cours | → staging temporaire |
| **Obsolète** | Information dépassée | → Supprimer |

---

## Étape 3 — Rapport avant modification

```
FICHIER AGENT DÉTECTÉ : <CLAUDE.md | AGENTS.md | GEMINI.md | ...>
  .ai/HARNESS.md : <existe | absent>

CONTENU À MIGRER
  → .ai/user/profile.md     : <liste>
  → .ai/user/preferences.md : <liste>
  → docs/                   : <liste>
  → supprimer               : <liste>
```

Attendre validation.

---

## Étape 4 — Exécution

1. Déplacer chaque contenu vers sa destination
2. Si `.ai/user/` n'existe pas : créer le dossier
3. Vérifier que `.ai/user/` est dans `.gitignore`
4. Ajouter au fichier agent existant (sans remplacer le contenu) une section de référence
   au Harness, positionnée en début de fichier après le titre principal :

```markdown
## Harness multi-agent

Ce projet utilise un Harness universel. Point d'entrée : `.ai/HARNESS.md`
```

---

## Règles

- Le fichier agent peut contenir du contenu projet propre à l'agent (règles, conventions, état)
- Le Harness s'y greffe via une section de référence — il ne remplace pas le fichier agent
- La logique partagée multi-agent vit dans `.ai/HARNESS.md`
- `.ai/user/` est toujours gitignored
- Aucun `CLAUDE.workflow.md` — concept supprimé
