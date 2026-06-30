# Règle — Documentation XML C#

## Principe général

La documentation XML décrit **ce que fait** un membre, pas **comment il le fait**. Le niveau de détail dépend du type de composant documenté.

---

## Interfaces

Une interface déclare un contrat : ce que la méthode accomplit et ce qu'elle retourne. Elle ne documente jamais les règles métier ni les exceptions levées.

```csharp
/// <summary>Retourne un utilisateur par son identifiant.</summary>
/// <param name="id">Identifiant de l'utilisateur.</param>
/// <returns>L'utilisateur correspondant, ou <c>null</c> s'il n'existe pas.</returns>
Task<User?> GetByIdAsync(Guid id);
```

**À éviter sur une interface :**
- Mentionner une règle métier (`"Lance 409 si une Diet est déjà active"`)
- Mentionner une exception (`"Lève ArgumentException si..."`)
- Décrire un comportement interne (`"Calcule le BMR avant de persister"`)

---

## Classes concrètes — Services

Le service documente les règles métier, les gardes, et les effets de bord. C'est ici que les `<exception>` ont leur place.

```csharp
/// <summary>Lance un DietPlan et crée une Diet active pour l'utilisateur.</summary>
/// <param name="userId">Identifiant de l'utilisateur.</param>
/// <param name="request">Données de lancement du plan.</param>
/// <returns>La Diet créée.</returns>
/// <exception cref="InvalidOperationException">Une Diet est déjà active pour cet utilisateur.</exception>
/// <exception cref="InvalidOperationException">Aucun WeightEntry n'existe pour cet utilisateur.</exception>
public async Task<DietResponse> LaunchAsync(Guid userId, LaunchDietPlanRequest request)
```

---

## Classes concrètes — Repositories

Le repository documente les conditions de retour et les effets de bord sur la persistance. Pas de règles métier.

```csharp
/// <summary>Retourne le régime actif de l'utilisateur.</summary>
/// <param name="userId">Identifiant de l'utilisateur.</param>
/// <returns>Le régime avec le statut <c>Active</c>, ou <c>null</c> si aucun.</returns>
public async Task<Diet?> GetActiveByUserIdAsync(Guid userId)
```

---

## Entités Domain

L'entité documente les invariants et les effets de mutation. Les constructeurs documentent les paramètres obligatoires.

```csharp
/// <summary>Crée une nouvelle Diet à partir d'un snapshot de DietPlan.</summary>
/// <param name="userId">Identifiant de l'utilisateur propriétaire.</param>
/// <param name="calorieTarget">Objectif calorique calculé au lancement.</param>
/// <exception cref="ArgumentException">userId est un Guid vide.</exception>
public Diet(Guid userId, int calorieTarget)
```

Les méthodes de mutation documentent ce qu'elles modifient :

```csharp
/// <summary>Archive le régime et enregistre la date de fin.</summary>
/// <param name="endDate">Date d'archivage du régime.</param>
public void Archive(DateOnly endDate)
```

---

## Récapitulatif

| Composant | `<summary>` | `<exception>` | Règles métier |
|---|---|---|---|
| Interface | Contrat (quoi) | ❌ | ❌ |
| Service | Comportement + effets | ✅ | ✅ |
| Repository | Persistance + retour | ❌ | ❌ |
| Entité Domain | Invariants + mutation | ✅ | ✅ |
