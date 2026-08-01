# Workflow Jira

Uniquement si Maxime le demande explicitement. Quand déclenché :

1. Passer la sous-tâche en `En cours`
2. Commiter avec `#NTR-XX`
3. Passer la sous-tâche en `Terminé`
4. Si doc externe citée → commentaire Jira avec le lien (`https://mgnetworking.github.io/docs-nutrition/`)

Instance : `maxime-ghalem.atlassian.net`

---

## Rédaction des commentaires

### Ce que le convertisseur Markdown casse

Le Markdown envoyé est converti en ADF côté Jira, et cette conversion **altère silencieusement**
certaines écritures — y compris à l'intérieur d'un bloc de code délimité par des accents graves.
Constaté le 2026-07-31 sur NTR-149 et NTR-123 :

| Écriture | Ce qui s'affiche | Conséquence |
|---|---|---|
| Double tiret bas — `Keycloak` + `__` + `Realm` | `Keycloak**Realm` | **le plus grave** : un nom de variable d'environnement copié depuis le commentaire est faux |
| Chevrons génériques — `RemoveAll<IHostedService>()` | `RemoveAll()` | le type disparaît, la phrase perd son sens |
| Tableau Markdown | lignes successives sans structure | illisible dès trois colonnes |
| Tiret bas simple en code — `client_credentials` | `client\_credentials` | barre oblique parasite |

### Comment écrire à la place

- **Variables d'environnement ASP.NET Core** : donner la clé de configuration (`Keycloak:Realm`) et
  rappeler en toutes lettres que le deux-points devient un double tiret bas. Ne jamais l'écrire
  littéralement.
- **Génériques** : reformuler — « `RemoveAll` appliqué à `IHostedService` ».
- **Tableaux** : les remplacer par des paragraphes ou une liste à puces.

### Vérifier après coup

La réponse de l'outil contient le corps tel qu'il a été enregistré. **Le relire** : c'est là que les
altérations se voient. Corriger avec `jira_edit_comment` plutôt que d'empiler un second commentaire.

---

## Propager ce qu'un ticket impose à un autre

Un critère d'acceptation laissé ouvert, ou une contrainte qu'une livraison fait peser sur un ticket
futur, doit être écrit **dans le ticket qui devra l'honorer** — pas seulement mentionné dans celui
qu'on ferme. Sinon l'information meurt avec la fermeture.

Le détail va dans le ticket qui fera le travail (souvent une sous-tâche) ; un résumé court avec
renvoi va sur son parent. Et le ticket qu'on ferme indique où la suite a été consignée.
