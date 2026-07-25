# Règle — Documentation projet (Markdown)

> Cette règle concerne la **documentation du projet** (features, workflows, annexes).
> Pour la documentation XML du code C#, voir `xml-documentation.md`.

---

## Principe transverse — documenter les briques ne suffit jamais

Une documentation qui décrit chaque outil isolément **ne permet pas de comprendre le
fonctionnement**. Il faut toujours un document qui raconte l'**assemblage**.

Symptôme à reconnaître : le lecteur dit *« j'ai l'impression d'avoir deux choses séparées »*.
C'est le signe qu'il manque le document d'assemblage, pas que les documents existants sont mauvais.

### Les trois niveaux de lecture

| Niveau | Répond à | Emplacement |
|---|---|---|
| **1. Feature** | *quoi* et *pourquoi* — le besoin, le périmètre | `features/utilisateur/` ou `features/interne/` |
| **2. Workflow** | *comment ça marche*, de bout en bout | `annexes/workflow-<sujet>.md` |
| **3. Référence** | une brique isolée (un outil, une source de données) | `annexes/infrastructure-<brique>.md` |

**Règles de rattachement :**

- Chaque document **annonce sa portée en tête**, dans un bandeau `>`.
- Un document de niveau 3 **renvoie vers son niveau 2** (« pour le fonctionnement d'ensemble, lire… »).
- Un document de niveau 1 met le **niveau 2 en premier** dans ses références — pas les briques.
- Une brique partagée par plusieurs fonctionnalités (ex. Hangfire, qui sert l'import OFF *et* la
  purge RGPD) le dit explicitement : elle ne doit pas paraître dédiée à l'une d'elles.

---

## Gabarit — fiche feature (niveau 1)

```markdown
# <Nom>
**Ajouté le :** AAAA-MM-JJ
**Type :** Utilisateur | Interne
**Référence spec :** <le workflow de niveau 2, en premier>

> Bandeau de portée : à quoi sert cette fiche, à quoi elle ne sert pas.

## Objectif
## Qui l'utilise
## Quand
## Ce qu'elle fait
## Ce qu'elle ne fait pas
## Limite connue          (si applicable — ne jamais la taire)
## Endpoints              (si applicable)
## Dépendances
```

**Une fiche = un seul type.** Si une notion mêle usage utilisateur et fonctionnement interne
(ex. RGPD : droits de l'utilisateur / job de purge), la scinder en deux fiches.

---

## Gabarit — workflow (niveau 2)

C'est le document le plus important : celui qu'on lit pour comprendre.

```markdown
# Workflow — <Nom>
**Ajouté le :** · **Feature associée :** · **Ticket :**

> Bandeau : ce document explique le fonctionnement de bout en bout ; les annexes voisines
> ne décrivent chacune qu'une brique.

## 1. Vue d'ensemble            schéma ASCII des déclencheurs et des flux
## 2. Qui fait quoi             arborescence + tableau « classe → responsabilité unique »
## 3. Le parcours de <X>        étape par étape, du déclencheur au résultat
## 4. Décisions et leurs raisons   tableau « décision → pourquoi », y compris options écartées
## 5. Ce qui n'est pas couvert   limites connues, avec leur statut
## 6. État de confiance         ✅ testé / ⚠️ vérifié manuellement / ❌ non vérifié
## 7. Configuration
## 8. Où creuser                renvois vers les niveaux 1 et 3
```

### Deux sections non négociables

**« Décisions et leurs raisons »** — une décision sans son *pourquoi* sera re-débattue à chaque
session. Mentionner l'option écartée et ce qu'elle aurait coûté.

**« État de confiance »** — distinguer explicitement :

| Marque | Signification |
|---|---|
| ✅ | couvert par des tests automatisés |
| ⚠️ | vérifié manuellement, ou décidé mais non implémenté |
| ❌ | non vérifié, ou identifié comme manquant |

C'est la section la plus utile : elle empêche de confondre *« c'est écrit »* et *« c'est éprouvé ».
Ne jamais l'omettre, même quand tout est vert.

---

## Gabarit — annexe de référence (niveau 3)

```markdown
# Infrastructure — <Brique>
**Ajouté le :**

> **Portée de ce document : <la brique>, et elle seule.**
> ➜ Pour le fonctionnement d'ensemble, lire `workflow-<sujet>.md`.

## Pourquoi <cette brique>      (l'alternative écartée)
## Comment elle fonctionne      (le modèle mental avant la configuration)
## Configuration
## <sections techniques>
## Voir aussi
```

---

## Règles d'écriture

- **Écrire ce qui est, pas ce qui devrait être.** Un écart constaté à l'implémentation se corrige
  dans la doc, avec une mention explicite (« corrigé après implémentation, NTR-XX »).
- **Ne jamais taire une limite connue.** Une limite documentée est une dette maîtrisée ; une limite
  tue est un piège.
- **Le code fait foi sur les identifiants** (noms de jobs, clés de configuration, noms de tables) :
  toujours vérifier dans le code avant d'écrire, jamais de mémoire.
- **Toute page ajoutée doit entrer dans la `nav:` de `mkdocs.yml`**, sinon elle n'est pas publiée.
- Vérifier les liens après tout déplacement — le build MkDocs tourne en `--strict`.
