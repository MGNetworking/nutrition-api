# create-adr <sujet>

Capture une décision architecturale en ADR permanent dans `.ai/decisions/`.

---

## Flux

1. Lire `.ai/decisions/README.md` — identifier le prochain numéro ADR
2. Créer `.ai/decisions/ADR-NNN-<sujet-kebab-case>.md`
3. Mettre à jour `.ai/decisions/README.md`

---

## Format ADR

```markdown
# ADR-NNN — <Sujet>

**Date** : <YYYY-MM-DD>
**Statut** : Accepté

## Contexte

<Quelle situation a nécessité cette décision ?>

## Décision

<Quelle décision a été prise ?>

## Alternatives considérées

- <Alternative 1> — rejetée car <raison>
- <Alternative 2> — rejetée car <raison>

## Conséquences

### Positives
- <liste>

### Négatives / contraintes
- <liste>
```

---

## Index `.ai/decisions/README.md`

```markdown
# Décisions architecturales

| ADR | Titre | Date | Statut |
|-----|-------|------|--------|
| ADR-001 | <titre> | <date> | Accepté |
```

---

## Règles

- Un ADR par décision — ne pas grouper des décisions indépendantes
- Statuts : Proposé, Accepté, Remplacé par ADR-NNN, Obsolète
- Ne jamais modifier un ADR accepté — créer un ADR de supersession
