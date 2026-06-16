# bootstrap-context

Initialise le Harness dans le projet courant.
Génère `.ai/` complet avec index de navigation vers la documentation.

> Prérequis : exécuter `format-agent-file` avant.

---

## Étape 1 — Lire le fichier agent

Identifier quel fichier d'intégration agent existe (CLAUDE.md, AGENTS.md, GEMINI.md...).
Vérifier qu'il contient une référence vers `.ai/HARNESS.md`.
Si non : signaler que `/harness:format-agent-file` doit être exécuté d'abord.

Identifier : racine du projet, fichier de suivi.

---

## Étape 2 — Config locale

Vérifier si `.ai/user/` existe.
Créer `.ai/user/profile.md` et `.ai/user/preferences.md` si absent.
Vérifier que `.ai/user/` est dans `.gitignore`.

---

## Étape 3 — Détecter la documentation

Explorer le projet pour identifier les sources documentaires.
Tester : `docs/`, `documentation/`, `specs/`, `wiki/`, `knowledge/`, `.`
Lister les fichiers markdown sous chaque source trouvée (noms uniquement).

---

## Étape 4 — Analyse physique

Explorer la structure des dossiers code et tests.
Détecter : architecture, stack, conventions, framework de test.

---

## Étape 5 — Rapport avant génération

```
DOCUMENTATION DÉTECTÉE
  Racines      : <liste des dossiers trouvés>
  Fichiers     : <N> fichiers markdown

MAPPING PROPOSÉ
  business      → <fichiers>
  architecture  → <fichiers>
  product       → <fichiers>
  integrations  → <fichiers>
  infrastructure→ <fichiers>
  quality       → <fichiers>

STRUCTURE PHYSIQUE
  code.root     : <chemin>
  tests.root    : <chemin>
  stack         : <détecté>
  architecture  : <détectée>
  test framework: <détecté>

.ai/user/     : <créé | existant> — gitignored <oui | non>
```

Attendre validation.

---

## Étapes 6 à 15 — Génération

6. `.ai/documentation-map.md`
7. `.ai/project-analysis.md`
8. `.ai/autonomy-policy.md`
9. `.ai/contexts/` — un fichier par catégorie ayant de la documentation
10. `.ai/knowledge/` — glossary.md, lessons-learned.md, known-pitfalls.md
11. `.ai/improvements/<YYYY-MM>.md`
12. `.ai/workflows/` — copier depuis `~/.claude/harness/workflows/` avec adaptation stack
13. `.ai/HARNESS.md`
14. `.claude/commands/` — adaptateurs minces si Claude Code détecté
15. Vérifier `.gitignore` contient `.ai/user/`

---

## Rapport final

```
CRÉÉS
.ai/
  HARNESS.md
  documentation-map.md    : <N> catégories, <N> racines
  project-analysis.md
  autonomy-policy.md
  contexts/               : <N> contextes
  decisions/README.md
  knowledge/              : 3 fichiers
  improvements/           : 1 fichier
  workflows/
    bootstrap/            : 3 fichiers
    development/          : 5 fichiers
    maintenance/          : 3 fichiers

.claude/commands/         : <N> adaptateurs  (si Claude Code)
.ai/user/                 : gitignored ✓
```
