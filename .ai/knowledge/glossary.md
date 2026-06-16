# Glossaire

## Domaine nutrition

| Terme | Définition |
|---|---|
| BMR | Basal Metabolic Rate — dépense énergétique au repos |
| TDEE | Total Daily Energy Expenditure — dépense totale journalière |
| Macros | Macronutriments : protéines, glucides, lipides (en grammes) |
| MacroGrams | Value Object encapsulant protéines/glucides/lipides calculés |
| DietPlan | Plan nutritionnel avec objectif calorique et macros |
| DietPlanTemplate | Modèle de plan pré-défini (ex : prise de masse, sèche) |
| MealEntry | Enregistrement d'un repas (MealItems + timestamp) |
| MealItem | Aliment consommé dans un repas (FoodItem + quantité) |
| FoodItem | Aliment avec valeurs nutritionnelles (source : Open Food Facts) |
| WeightEntry | Entrée de poids journalière |
| NutritionBilan | Bilan journalier/hebdomadaire calculé |

## Architecture

| Terme | Définition |
|---|---|
| Aggregate Root | Entité racine d'un agrégat DDD — seul point d'accès |
| Value Object | Objet défini par sa valeur, immuable (ex : MacroGrams) |
| Guard | Validateur transversal (ex : SubscriptionGuard) |
| Engine / Moteur | Composant Strategy + Factory orchestrant un calcul complexe |
| IBmrStrategy | Interface stratégie de calcul BMR |
| NutritionCalculator | Moteur principal de calcul BMR/TDEE/macros |

## Abréviations

| Abréviation | Signification |
|---|---|
| NTR | Préfixe des tickets Jira du projet |
| OFF | Open Food Facts |
| RGPD | Règlement Général sur la Protection des Données |
| DDD | Domain-Driven Design |
