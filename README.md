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
dotnet run --launch-profile http
```

Accès : [http://localhost:5089](http://localhost:5089)
Swagger : [http://localhost:5089/swagger](http://localhost:5089/swagger)

---

### HTTPS (port 7181)

```bash
# Générer le certificat de développement (une seule fois)
dotnet dev-certs https --trust

dotnet run --launch-profile https
```

Accès : [https://localhost:7181](https://localhost:7181)
Swagger : [https://localhost:7181/swagger](https://localhost:7181/swagger)

---

### Docker (ports 8080 / 8081)

```bash
docker build -t nutrition-api .
docker run -p 8080:8080 -p 8081:8081 nutrition-api
```

| Service      | URL                                   |
| ------------ | ------------------------------------- |
| API HTTP     | http://localhost:8080                 |
| API HTTPS    | https://localhost:8081                |
| Swagger      | http://localhost:8080/swagger         |

---

## Version

Voir [CHANGELOG.md](CHANGELOG.md) — géré automatiquement par [Release Please](https://github.com/googleapis/release-please).
