# nutrition-api

[![CI — Pull request](https://github.com/MGNetworking/nutrition-api/actions/workflows/ci-pr.yml/badge.svg)](https://github.com/MGNetworking/nutrition-api/actions/workflows/ci-pr.yml)
[![CI — Release](https://github.com/MGNetworking/nutrition-api/actions/workflows/ci-release.yml/badge.svg)](https://github.com/MGNetworking/nutrition-api/actions/workflows/ci-release.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=MGNetworking_nutrition-api&metric=coverage)](https://sonarcloud.io/summary/new_code?id=MGNetworking_nutrition-api)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=MGNetworking_nutrition-api&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=MGNetworking_nutrition-api)
[![Documentation](https://img.shields.io/badge/docs-GitHub_Pages-blue)](https://mgnetworking.github.io/docs-nutrition/)
[![Release](https://img.shields.io/github/v/release/MGNetworking/nutrition-api)](https://github.com/MGNetworking/nutrition-api/releases)
[![Licence](https://img.shields.io/badge/licence-FSL--1.1--ALv2-blue)](LICENSE)

> **Pas de badge Dependabot, et c'est délibéré.** Dependabot est actif — voir
> [`.github/dependabot.yml`](.github/dependabot.yml) et les
> [mises à jour proposées](https://github.com/MGNetworking/nutrition-api/network/updates). Mais il
> n'existe plus de badge d'état : GitHub a supprimé le service qui le servait en absorbant
> Dependabot, sans le remplacer. Un badge statique afficherait « enabled » même le jour où la
> configuration serait supprimée — une décoration, pas une mesure.

API SaaS de gestion nutritionnelle — backend ASP.NET Core 10, architecture DDD en quatre couches.

---

## Sommaire

- [Aperçu](#aperçu)
- [Stack technique](#stack-technique)
- [Architecture](#architecture)
- [Démarrage rapide](#démarrage-rapide)
- [Environnements](#environnements)
- [Base de données](#base-de-données)
- [Tester l'API](#tester-lapi)
- [Tests automatisés](#tests-automatisés)
- [Structure du dépôt](#structure-du-dépôt)
- [Documentation](#documentation)

---

## Aperçu

Nutrition API est le backend d'un SaaS de suivi nutritionnel. Il permet à un utilisateur de définir
des plans diététiques, de lancer un régime avec un objectif calorique calculé, de saisir ses repas à
partir d'un catalogue d'aliments, et de suivre sa progression.

**Fonctionnalités principales**

| Domaine | Ce qui est couvert |
|---|---|
| Profil utilisateur | Données biométriques, calcul BMR/TDEE, historique de pesées |
| Plans diététiques | Plans personnels et templates partagés selon le palier d'abonnement |
| Régimes | Lancement depuis un plan (snapshot), suivi, archivage |
| Repas | Composition à partir du catalogue, calcul nutritionnel automatique |
| Aliments | Recherche cache-first, catalogue alimenté par Open Food Facts |
| Bilan | Agrégation des apports face aux objectifs sur une période |
| Back-office | KPIs, état des jobs planifiés, gestion des templates |
| RGPD | Suppression avec grace period, réactivation, export des données |

Trois paliers d'abonnement — **Free**, **Pro**, **Business** — déterminent les quotas et l'accès aux
fonctionnalités.

**Limites connues à ce stade**

| Limite | Détail |
|---|---|
| Purge RGPD | Le service et les endpoints existent ; le job planifié qui supprime définitivement les comptes à l'expiration de la grace period n'est pas implémenté. |
| Envoi d'e-mails | `IEmailService` est déclarée côté Application, sans implémentation Infrastructure ni enregistrement dans le conteneur. |
| Administration Keycloak | `IKeycloakAdminService` est dans le même état. |

---

## Stack technique

| Composant | Technologie | Version |
|---|---|---|
| Runtime | .NET / ASP.NET Core | 10 |
| Base de données | PostgreSQL | 16 |
| ORM | Entity Framework Core | 10 |
| Cache | Redis | 7 |
| Authentification | Keycloak (OIDC / JWT) | 26 |
| Tâches planifiées | Hangfire | 1.8 |
| Documentation d'API | Swagger / OpenAPI (Swashbuckle) | 7.2 |
| Tests | xUnit, Moq, coverlet | — |

---

## Architecture

Quatre couches, selon les principes du Domain-Driven Design.

| Couche | Responsabilité |
|---|---|
| **Domain** | Entités, value objects, invariants métier. Aucune dépendance. |
| **Application** | Services applicatifs, DTOs, **toutes les interfaces**. Orchestration. |
| **Infrastructure** | EF Core, Redis, Hangfire — les implémentations techniques. |
| **Api** | Controllers, middlewares, authentification, pipeline HTTP. |

**Sens des dépendances**

```
Api  ──────────────►  Application  ──────────────►  Domain
 │                         ▲                          ▲
 │                         │                          │
 └──►  Infrastructure  ────┘──────────────────────────┘
```

Le point clé : **Application ne référence jamais Infrastructure**. Les interfaces de persistance
(`IUserRepository`) et de services externes (`IFoodCacheService`) sont déclarées dans Application ;
Infrastructure vient s'y brancher en sens inverse. La couche métier ignore donc EF Core, Redis et
Hangfire.

---

## Démarrage rapide

**Prérequis** : [.NET 10 SDK](https://dotnet.microsoft.com/download),
[Docker Desktop](https://www.docker.com/products/docker-desktop), et l'outil EF Core :

```bash
dotnet tool install --global dotnet-ef
```

**Lancer le projet**

```bash
./scripts/dev-up.sh                          # PostgreSQL, Redis, Keycloak + migrations + données de test
dotnet run --project src/NutritionApi.Api    # l'API sur http://localhost:5099
```

Swagger est alors disponible sur [http://localhost:5099/swagger](http://localhost:5099/swagger).

**Arrêter**

```bash
./scripts/dev-down.sh              # conserve les données
./scripts/dev-down.sh --volumes    # supprime les données
./scripts/dev-reset.sh             # repart d'une base vierge
```

**Tests d'intégration externe (niveau 3)**

```bash
./scripts/test-integration.sh                 # monte la pile puis lance les tests Level=3
./scripts/test-integration.sh --no-build      # réutilise la compilation existante
```

Le script monte la même pile que `dev-up.sh` et attend les trois healthchecks, mais ne charge pas
`seed-dev.sql` : chaque test crée sa propre base éphémère et sème ses données. Voir
[Écrire un test de niveau 3](https://mgnetworking.github.io/docs-nutrition/backend/qualite/tests-niveau-3/).

---

## Environnements

Deux façons de faire tourner l'API en local, selon le besoin.

| | **Dev** | **Docker** |
|---|---|---|
| L'API tourne… | sur la machine hôte | dans un conteneur |
| Lancement | `dotnet run` ou l'IDE | `./scripts/docker-up.sh` |
| Port de l'API | **5099** | **5100** |
| Configuration chargée | `appsettings.Development.json` | `appsettings.Docker.json` |
| Usage | développement quotidien, débogage | valider l'image, reproduire la production |

Les deux modes partagent les mêmes services conteneurisés :

| Service | Adresse | Identifiants |
|---|---|---|
| PostgreSQL | `localhost:5445` | base `nutrition_dev`, `postgres` / `postgres` |
| Redis | `localhost:6336` | — |
| Keycloak | [http://localhost:8778](http://localhost:8778) | console admin `admin` / `admin` |

> Les ports externes sont volontairement décalés pour ne pas entrer en conflit avec des instances
> déjà présentes sur le poste. Les ports internes aux conteneurs restent standards.

**Comptes de test**, provisionnés automatiquement par le realm Keycloak — mot de passe `test` :

| Compte | Rôle Keycloak | Abonnement |
|---|---|---|
| `test-user` | `user` | Free |
| `test-pro` | `user` | Pro |
| `test-admin` | `admin` | Free |

Détail complet du setup local : [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Base de données

Le schéma est géré par les migrations EF Core, versionnées dans
`src/NutritionApi.Infrastructure/Persistence/Migrations/`.

```bash
# Appliquer les migrations
dotnet ef database update \
  --project src/NutritionApi.Infrastructure \
  --startup-project src/NutritionApi.Api

# Créer une migration après modification du modèle
dotnet ef migrations add NomDuChangement \
  --project src/NutritionApi.Infrastructure \
  --startup-project src/NutritionApi.Api \
  --output-dir Persistence/Migrations
```

`./scripts/dev-up.sh` applique les migrations automatiquement, puis charge `seed-dev.sql` — des
données métier de test (utilisateurs, pesées, plans, aliments, repas) réservées au développement.

> L'application n'applique **jamais** les migrations à son démarrage : la base n'évolue qu'au
> déploiement. En mode Docker, un conteneur one-shot s'en charge avant que l'API ne démarre.

Configuration détaillée : [CONFIGURATION.md](CONFIGURATION.md).

---

## Tester l'API

### Swagger

[http://localhost:5099/swagger](http://localhost:5099/swagger) — chaque endpoint documente son
résumé, ses codes de réponse et le schéma de ses DTOs. Le bouton **Authorize** accepte un jeton JWT.

### Collection Postman

Le dossier `postman/` contient une collection couvrant les 36 endpoints REST groupés par ressource, plus un dossier « Plateforme » pour les surfaces qui n'en sont pas — dashboard Hangfire et spec OpenAPI :

| Fichier | Usage |
|---|---|
| `nutrition-api.postman_collection.json` | La collection |
| `nutrition-dev.postman_environment.json` | Mode dev — port 5099 |
| `nutrition-docker.postman_environment.json` | Mode Docker — port 5100 |

Importer les trois fichiers, sélectionner l'environnement correspondant au mode de lancement, puis
exécuter une requête du dossier **Authentification** : le jeton est enregistré automatiquement et
appliqué à toutes les requêtes suivantes.

### Obtenir un jeton en ligne de commande

```bash
curl -X POST "http://localhost:8778/realms/nutrition/protocol/openid-connect/token" \
  -d "client_id=nutrition-api" -d "grant_type=password" \
  -d "username=test-user" -d "password=test"
```

### Sondes de santé

Deux points de terminaison anonymes, qui répondent à deux questions distinctes.

| Chemin | Question | Consulte | Réponses |
|---|---|---|---|
| `/health` | suis-je vivant ? | **rien** | 200 tant que le processus répond |
| `/health/ready` | suis-je en état de servir ? | PostgreSQL, clés de signature, Redis | 200 ou 503 |

```bash
curl -s http://localhost:5099/health/ready | jq
```

Le corps détaille chaque dépendance, son statut et sa durée — un 503 dit lequel des trois manque,
là où un code seul laisserait chercher.

**Ce qui pèse dans le verdict d'aptitude**

| Dépendance | Poids | Pourquoi |
|---|---|---|
| PostgreSQL | bloquante | sans base, l'application ne sait rien répondre |
| Clés de signature | bloquante | sans elles, tous les jetons seraient refusés |
| Redis | signalée, non bloquante | le cache est un accélérateur : son absence dégrade, elle ne rend pas inapte |

> **La sonde des clés demande « ai-je des clés ? », pas « Keycloak répond-il ? »** La validation des
> jetons est locale, à partir des clés publiques mises en cache : une instance qui les a obtenues
> continue de servir pendant une coupure du serveur d'identité. Sonder sa joignabilité aurait retiré
> du service une flotte entière — toutes ses instances répondant correctement — le jour d'un
> redémarrage de Keycloak.

> **`/health` ne consulte volontairement aucune dépendance.** C'est cette sonde qu'un orchestrateur
> branche sur la vivacité. Si elle échouait faute de dépendance, le conteneur serait tué au lieu
> d'attendre — et une instance démarrée avant son serveur d'identité entrerait dans une boucle de
> redémarrage au lieu de patienter hors du service, puis de le rejoindre d'elle-même.

Les quatre comportements ci-dessus sont éprouvés au niveau 3 par `Startup/HealthProbeTest.cs`, qui
coupe réellement Keycloak et Redis.

---

## Tests automatisés

La stratégie de test s'organise en quatre niveaux, chacun répondant à une question différente.
Un même comportement n'est jamais vérifié à deux niveaux.

| Niveau | Question | Outil | Docker | État |
|---|---|---|---|---|
| **1 — Unitaires** | Ma logique métier est-elle correcte ? | xUnit + Moq | Non | ✅ en place |
| **2 — Intégration interne** | Mes composants fonctionnent-ils ensemble ? | WebApplicationFactory | Non | 🔨 socle en place (NTR-134) |
| **3 — Intégration externe** | Mon application dialogue-t-elle avec ses dépendances réelles ? | WebApplicationFactory + docker-compose | Oui | ❌ non implémenté |
| **4 — Smoke tests** | Le système déployé fonctionne-t-il ? | Client HTTP | Cluster | ❌ non implémenté |

> **Le socle du niveau 2 existe depuis NTR-134** — `Integration/Fixtures/ApiFactory.cs` et
> `TestAuthHandler.cs` — mais aucun test métier n'a encore été écrit : c'est l'objet des tickets
> NTR-105 à NTR-111. Les fichiers de stubs `[Fact(Skip = …)]` restent en place, porteurs des
> marqueur de niveau porté par chaque classe de test.
>
> ➜ **[Écrire un test de niveau 2](https://mgnetworking.github.io/docs-nutrition/backend/qualite/tests-niveau-2/)** — fonctionnement du socle, rôle de chaque classe et gabarit de test.

**Niveau 1 — Unitaires.** Entités du domaine, invariants, services applicatifs avec repositories
mockés. Rapides, sans dépendance externe. C'est le niveau le plus fourni, et le seul couvert par la
CI.

**Niveau 2 — Intégration interne.** *Socle en place.* Routing, sérialisation, middlewares, policies
d'autorisation. L'application est montée en mémoire par `ApiFactory`, et les onze interfaces de
frontière — repositories, unité de travail, cache, Keycloak Admin, supervision — sont remplacées par
des doublures. L'authentification passe par `TestAuthHandler`, qui construit l'identité depuis des
en-têtes : sans Keycloak, aucun jeton signé n'est productible.

**Niveau 3 — Intégration externe.** *Prévu.* Les trois services du `docker-compose.yml` seront
réels : validation des requêtes EF Core contre PostgreSQL, du TTL du cache Redis, et de la chaîne
JWT complète (issuer, audience, signature) contre Keycloak.

**Niveau 4 — Smoke tests.** *Prévu.* À exécuter après déploiement sur l'environnement réel :
l'application répond, les connexions sont actives, la configuration est cohérente. Pas de scénario
métier.

### Commandes

```bash
# Tous les tests
dotnet test

# Uniquement les tests d'intégration (niveau 2)
dotnet test --filter "FullyQualifiedName~Integration"

# Tout sauf eux — utile pour garder une boucle rapide
dotnet test --filter "FullyQualifiedName!~Integration"

# Avec couverture de code
dotnet test --settings tests/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage

# Rapport HTML (outil à installer une seule fois)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" \
  -reporttypes:Html -classfilters:"-NutritionApi.Application.DTOS.*"
```

Le rapport est généré dans `coverage/report/index.html`.

**Comment les niveaux se sélectionnent**

`--filter` est une option native de `dotnet test`. `FullyQualifiedName` désigne le nom complet du
test, **namespace compris**, et `~` signifie « contient » :

```
NutritionApi.Api.Tests.Level2.ApiFactoryTest.ProtectedEndpoint_WithoutIdentity_Returns401
└──────────────── namespace ─────────────────┘ └── classe ──┘ └────────── méthode ──────────┘
```

La sélection par namespace reste possible, mais ce n'est plus le mécanisme principal.

**Chaque test porte son niveau** — `[Trait("Level", "1")]` à `[Trait("Level", "3")]`, posé sur la
classe. C'est ce marqueur qui pilote les filtres, et lui seul :

```bash
dotnet test --filter "Level=1"     # unitaires
dotnet test --filter "Level=2"     # pipeline HTTP, doublures aux frontières
dotnet test --filter "Level=3"     # PostgreSQL, Redis et Keycloak réels
dotnet test --filter "Level!=3"    # tout ce qui n'exige pas Docker
```

Le rangement le double : un dossier par niveau dans `Api.Tests` (`Level1/`, `Level2/`), et un projet
dédié pour le niveau 3.

Pourquoi un marqueur plutôt que le seul namespace : un test de niveau 3 rangé par erreur ailleurs
resterait exclu de la CI unitaire, là
où un filtre par namespace le laisserait passer — et faire échouer une CI sans Docker.

L'intérêt pratique est double : lancer les tests d'intégration seuls quand on travaille dessus, et
surtout **les exclure** pendant le développement courant, puisqu'ils démarrent l'API et coûtent
plusieurs dizaines de secondes là où les tests unitaires se comptent en millisecondes.

**Seuils de couverture par couche**

Déclarés dans `tests/coverage.runsettings`, en ligne **et** en branche. C'est la source unique :
aucun seuil n'est écrit ailleurs, ni dans un workflow, ni dans ce fichier.

| Couche | Seuil |
|---|---|
| Domain | 90 % |
| Application | 80 % |
| Infrastructure | 70 % |
| Api | 70 % |

Sont exclus du calcul : les DTOs, `Program.cs`, `DependencyInjection.cs`, les projets de tests et
tout membre marqué `[ExcludeFromCodeCoverage]`.

Le contrôle est assuré par `scripts/check-coverage.sh`, qui lit ces seuils et les compare au
rapport **fusionné** — niveaux 1, 2 et 3 réunis. Le job `coverage` de `ci-pr.yml` l'exécute à
chaque pull request vers `dev`, et c'est lui, et lui seul, qui fait échouer la CI sur la
couverture. Le script s'utilise aussi en local :

```bash
./scripts/check-coverage.sh coverage/report/Cobertura.xml
```

> **Pourquoi la fusion importe.** Mesurer une couche sur une partie seulement de ses tests donne
> un chiffre faux : Infrastructure tombe à 7,9 % quand on écarte le niveau 3, puisque ce sont ces
> tests-là qui traversent les dépôts EF Core et le cache Redis.

> **Corrigé le 2026-08-02 (NTR-132).** Cette section affirmait auparavant que les seuils étaient
> contrôlés à chaque pull request. Ils ne l'étaient pas : `coverlet.collector` ne lit pas le bloc
> `<Thresholds>` d'un runsettings — seule l'intégration MSBuild de Coverlet gère les seuils — et
> l'action de commentaire de PR laisse `fail_below_threshold` à `false` par défaut. Aucune barrière
> n'existait.

> Le badge **Coverage** en tête de ce fichier concerne l'affichage public du taux, qui reste à
> brancher — voir NTR-120.

---

## Structure du dépôt

```
nutrition-api/
├── src/
│   ├── NutritionApi.Domain/           Entités, value objects, enums — aucune dépendance
│   ├── NutritionApi.Application/      Services, DTOs, interfaces
│   ├── NutritionApi.Infrastructure/   EF Core, migrations, repositories, cache, jobs
│   └── NutritionApi.Api/              Controllers, middlewares, Program.cs
├── tests/                             Un projet de tests par couche, un pour le niveau 3,
│                                      + coverage.runsettings
├── scripts/                           Scripts d'environnement (dev-up, dev-down, dev-reset,
│                                      docker-up, test-integration, lib.sh)
├── keycloak/                          realm-export.json — realm, client, rôles, comptes de test
├── postman/                           Collection et environnements Postman
├── .github/workflows/                 CI — tests unitaires, intégration externe, release,
│                                      déploiement, Release Please
├── .claude/                           Règles et skills Claude Code
├── nutrition-api.slnx                 Solution
├── docker-compose.yml                 PostgreSQL, Redis, Keycloak (+ API sous profil « full »)
├── Dockerfile.migrations              Conteneur one-shot d'application des migrations
├── release-please-config.json         Versionnage et CHANGELOG automatisés
├── version.txt                        Version courante, tenue par Release Please
├── CLAUDE.md                          Contexte projet pour Claude Code
├── LICENSE                            FSL-1.1-ALv2
└── seed-dev.sql                       Données de test — développement uniquement
```

---

## Documentation

| Document | Contenu |
|---|---|
| [Documentation du projet](https://mgnetworking.github.io/docs-nutrition/) | Design des quatre couches, règles métier, features, backlog |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Environnement local, workflow Git, conventions de commit |
| [CONFIGURATION.md](CONFIGURATION.md) | Fichiers de configuration, variables d'environnement, installation |
| [CHANGELOG.md](CHANGELOG.md) | Historique des versions |
| [LICENSE](LICENSE) | Conditions d'utilisation — FSL-1.1-ALv2 |
