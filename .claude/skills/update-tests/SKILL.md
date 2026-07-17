---
name: update-tests
description: Crée ou met à jour la classe de test unitaire d'un service Application (mocks Strict, fixtures, couverture nominal/limites/erreurs). À utiliser quand Maxime demande de créer ou mettre à jour les tests d'une classe de service.
allowed-tools: Read, Glob, Grep, Write, Edit
---

# Skill — update-tests

Crée ou met à jour une classe de test unitaire pour une classe de service.

## Quand utiliser

- Créer la classe de test d'un nouveau service Application (ex: `AdminService` → `AdminServiceTest`)
- Resynchroniser les tests après modification d'un service existant
- Régénérer les tests d'une seule méthode (ex: `/update-tests NutritionService GetBilanAsync`)

**Ne pas utiliser pour :**
- Les tests d'entités Domain ou de Value Objects
- Les tests d'intégration (Infrastructure, EF Core)
- Modifier l'implémentation du service — hors périmètre strict

## Usage

```
/update-tests <ClassName> [MethodName]
```

- `<ClassName>` — nom de la classe cible (ex: `FoodItemService`)
- `[MethodName]` — optionnel — méthode ciblée (ex: `GetSavedAsync`)

---

## Instructions

### 1. Localiser la classe cible

Chercher `<ClassName>.cs` dans `src/`. Si non trouvé, signaler l'erreur et arrêter.

### 2. Localiser la classe de test

Chercher `<ClassName>Test.cs` dans `tests/`.

**Si non trouvée :**
- Informer que la classe de test n'existe pas
- Demander confirmation du nom avant création pour éviter les doublons
- Le nom attendu est `<ClassName>Test.cs` dans `tests/NutritionApi.Application.Tests/`

**Si trouvée :**
- Lire son contenu complet avant toute modification

### 3. Lire l'implémentation

Lire `<ClassName>.cs` uniquement pour comprendre ce que les tests doivent couvrir :
- Les dépendances injectées (constructeur)
- Les méthodes publiques et leur signature
- La logique métier — gardes, flux, cas d'erreur

**Périmètre strict : ne jamais modifier `<ClassName>.cs`. Si un écart est détecté entre l'implémentation et les tests, le signaler à Maxime et attendre sa décision avant toute action.**

### 4. Choisir le mode

#### Mode A — Création (classe de test absente)

Créer la classe de test complète :
- Mocks `MockBehavior.Strict` pour chaque dépendance
- `SubscriptionGuard` instancié directement (pas mocké)
- Fixtures `Build***()` pour construire les entités Domain valides
- Couvrir chaque méthode : nominal + cas limites + cas d'erreur
- Respecter la rule TDD : les tests doivent être en état **Red**

#### Mode B — Mise à jour complète (MethodName absent)

Relire l'implémentation complète et comparer avec les tests existants :
- Conserver les tests encore valides
- Mettre à jour les tests dont la logique a changé
- Ajouter les tests manquants pour les nouvelles méthodes ou nouveaux comportements

#### Mode C — Mise à jour ciblée (MethodName fourni)

- Identifier les tests existants pour `<MethodName>` (commentaire `// --- MethodName ---`)
- Lire uniquement la méthode ciblée dans l'implémentation
- Remplacer uniquement les tests de cette méthode
- Ne pas toucher les tests des autres méthodes

### 5. Checklist de génération

Avant de terminer, vérifier chaque point :

- [ ] Un test = un comportement = une assertion principale
- [ ] Nommage : `MethodAsync_ShouldXxx_WhenYyy`
- [ ] `MockBehavior.Strict` sur chaque mock — tout appel non configuré lève une exception
- [ ] `SubscriptionGuard` instancié directement (pas mocké)
- [ ] Les tests **compilent** mais sont en état **Red** si l'implémentation n'est pas encore complète
- [ ] Aucun test en `skip` ou commenté sans explication

### 6. Référence

Consulter `.claude/rules/tdd.md` pour le cycle Red/Green/Refactor et la couverture attendue.
