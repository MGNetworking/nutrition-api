# Conventions de code

Architecture : **DDD 4 couches** — `Domain / Application / Infrastructure / Api`

---

## Workflow API-first — ordre obligatoire

```
Interfaces → API layer → Application layer → Infrastructure layer
```

Avant chaque ticket API : lire la section du controller dans `design-api.md` (table des routes) — pas seulement l'interface. L'interface est un contrat technique, pas la source de vérité des routes.

Trois frontières d'interface :
- API → Application : `IXxxService` dans `Application/Interfaces/Services/`
- Application → Infrastructure : `IXxxRepository` dans `Application/Interfaces/Repositories/`
- Application → Services externes : `IXxxService` dans `Application/Interfaces/ExternalServices/`

---

## Arborescence — décision avant création

Créer des fichiers est une décision d'architecture, pas un détail d'exécution.

**Avant d'écrire le premier fichier**, présenter l'arborescence cible et la faire valider si
l'une de ces conditions est vraie :

- l'implémentation crée **plus de 2 fichiers** ;
- elle ajoute dans un dossier existant des fichiers d'une **nature différente** de ceux déjà
  présents (ex. : un lecteur de fichier dans un dossier de jobs) ;
- elle porte le contenu d'un dossier à **plus de 5 fichiers**.

Présenter l'arborescence comme un **choix**, avec son alternative — jamais comme une simple liste
de fichiers à créer. Un dossier doit pouvoir se lire ainsi : « tout ce qui est ici appartient à X,
et rien d'autre ».

La structure décrite dans la doc de design est un **point de départ, pas une contrainte** : si
l'implémentation réelle déborde de ce qu'elle prévoyait, le signaler **avant** de coder.

**Exemple de référence (NTR-55)** — le moteur de jobs a été séparé des jobs métier :

```
Infrastructure/
├── Scheduling/          ← mécanique partagée (supervision, filtre dashboard) — pas des jobs
└── Jobs/
    └── <NomDuJob>/      ← un dossier par job, avec tout ce qui lui appartient
```

---

## Pattern constructeur (entités Domain)

- `Id` → `Guid.NewGuid()` directement dans le constructeur
- `UserId` → validation `Guid.Empty` + assignation directement dans le constructeur
- Propriétés mutables (`Name`, `MealType`…) → déléguer à `Update***()` depuis le constructeur
- Valeurs fixes à la création (`IsSaved = false`, `MealItems = new List<>()`) → assignation directe

La validation n'est écrite qu'une seule fois — dans `Update***` — le constructeur appelle ces méthodes.

---

## Pattern exceptions (gardes)

- `< 0` → `ArgumentOutOfRangeException.ThrowIfNegative(x)`
- `<= 0` → `ArgumentOutOfRangeException.ThrowIfNegativeOrZero(x)`
- Enum `== Unknown`, `Guid.Empty`, invariant combiné → `ArgumentException($"... Received: {value}", nameof(value))`

---

## Pattern DTO Response

Le DTO Response porte son propre mapping via `static From(Entity entity)`. Le service ne mappe pas.

```csharp
// ✅ Sur le DTO Response
public static UserProfileResponse From(User user) { ... }

// ❌ Pas dans le service
var dto = new UserProfileResponse { ... };
```

Le DTO Request ne porte pas de `ToXxx()` — le service extrait les primitives lui-même.
`using NutritionApi.Domain.Entity;` dans un DTO Response est accepté.
