# nutrition-api

API SaaS de gestion nutritionnelle — backend ASP.NET Core 10.

---

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/products/docker-desktop)

---

## Lancer le projet

### HTTP (port 5089)

```bash
cd src/NutritionApi.Api
dotnet run --launch-profile http
```

Accès : [http://localhost:5089](http://localhost:5089)
Swagger : [http://localhost:5089/swagger](http://localhost:5089/swagger)

---

### HTTPS (port 7181)

```bash
# Générer le certificat de développement (une seule fois)
dotnet dev-certs https --trust

cd src/NutritionApi.Api
dotnet run --launch-profile https
```

Accès : [https://localhost:7181](https://localhost:7181)
Swagger : [https://localhost:7181/swagger](https://localhost:7181/swagger)

---

### Docker (ports 8080 / 8081)

```bash
docker build -f src/NutritionApi.Api/Dockerfile -t nutrition-api .
docker run -p 8080:8080 -p 8081:8081 nutrition-api
```

| Service      | URL                                   |
| ------------ | ------------------------------------- |
| API HTTP     | http://localhost:8080                 |
| API HTTPS    | https://localhost:8081                |
| Swagger      | http://localhost:8080/swagger         |

---

## Tests

### Lancer les tests

```bash
dotnet test
```

### Lancer les tests avec couverture de code

```bash
dotnet test --settings tests/coverage.runsettings --collect:"XPlat Code Coverage" --results-directory ./coverage
```

### Générer le rapport HTML

```bash
# Installer ReportGenerator (une seule fois, outil global)
dotnet tool install -g dotnet-reportgenerator-globaltool

# Générer le rapport
reportgenerator -reports:"coverage/**/coverage.cobertura.xml" -targetdir:"coverage/report" -reporttypes:Html
```

Le rapport est généré dans `coverage/report/index.html`.

**Seuils minimum par couche :**

| Couche | Seuil |
|---|---|
| Domain | 90 % |
| Application | 80 % |
| Infrastructure | 70 % |
| API | 70 % |

---

## Gestion des branches

Ce projet suit un workflow `feature/* → dev → prod → main`.

- `dev` — intégration, toutes les features y sont mergées via squash PR
- `prod` — production, alimentée depuis `dev`, déclenche le déploiement VPS
- `main` — releases stables taguées (`vX.Y.Z`)

Voir [CONTRIBUTING.md](CONTRIBUTING.md) pour le workflow complet, les conventions de commit et les règles de protection de branches.

---

## Version

Voir [CHANGELOG.md](CHANGELOG.md) — géré automatiquement par [Release Please](https://github.com/googleapis/release-please).
