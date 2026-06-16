# Leçons apprises

## NTR-9 — DietPlansService + Moteur de calcul (2026-06)

- La `SubscriptionGuard` est un composant transversal : sa création a nécessité un arbitrage explicite avant implémentation — ne pas l'intégrer silencieusement dans un service.
- Le moteur de calcul (Strategy + Factory) doit être traité comme un composant architectural à part entière, documenté dans `annexes/concept-moteur-architecture.md` avant implémentation.
- Les DTOs `MacroDistributionDto` utilisent `int` (pas `float`) pour les grammes de macros — vérifier le type avant d'écrire les assertions de test.

## NTR-8 — RgpdService (2026-06)

- RgpdService doit être découplé de UserService — ne pas co-localiser les responsabilités RGPD dans le service utilisateur même si les entités sont liées.
