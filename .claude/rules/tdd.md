# Règle — Test-Driven Development (TDD)

## Principe

Aucun code de production n'est écrit sans qu'un test qui échoue existe d'abord.
Le cycle est invariable : **Red → Green → Refactor**.

---

## Cycle obligatoire

### 1. Red — écrire le test

- Écrire le(s) test(s) qui couvrent le comportement attendu
- Le test doit compiler mais **échouer** à ce stade
- Un test = un comportement = une assertion principale

### 2. Green — écrire le minimum de code

- Écrire uniquement le code nécessaire pour faire passer les tests
- Pas d'anticipation, pas de généralisation prématurée
- Le test passe — rien de plus

### 3. Refactor — nettoyer

- Améliorer la lisibilité sans changer le comportement
- Les tests restent verts après refactor

---

## Ordre des fichiers — non négociable

1. Créer ou modifier le fichier de **test en premier**
2. Créer ou modifier le fichier d'**implémentation ensuite**
3. Jamais l'inverse

---

## Couverture attendue par méthode

| Catégorie | Ce qu'il faut couvrir |
|---|---|
| Chemin nominal | Comportement attendu avec des entrées valides |
| Cas limites | Valeur nulle, liste vide, valeur aux bornes |
| Cas d'erreur | Exception attendue, état invalide, ownership violation |

---

## Ce qu'un agent ne doit jamais faire

- Écrire l'implémentation puis ajouter des tests après coup
- Écrire des tests qui passent sans avoir vérifié qu'ils échouaient
- Implémenter plus que ce que les tests exigent
- Laisser un test en `skip` ou commenté sans explication explicite

---

## Ordre des commits (si commits séparés par étape)

```
test(scope): ajouter tests XxxService.YyyAsync
feat(scope): implémenter XxxService.YyyAsync
```

Si le cycle est court et les deux fichiers sont commités ensemble :

```
feat(scope): implémenter XxxService.YyyAsync avec tests
```
