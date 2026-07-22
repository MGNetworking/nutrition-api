#!/usr/bin/env bash
#
# Prépare l'environnement de développement local.
#
#   ./scripts/dev-up.sh
#
# Démarre PostgreSQL, Redis et Keycloak, attend qu'ils soient prêts, applique les
# migrations EF Core puis charge les données de test.
#
# Le script NE LANCE PAS l'API : elle se lance depuis l'IDE avec le débogueur, ou
# par `dotnet run --project src/NutritionApi.Api`.
#
# Idempotent : peut être relancé sur un environnement déjà démarré.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

cd "$REPO_ROOT"

require_docker
require_dotnet_ef

info "Démarrage des services (PostgreSQL, Redis, Keycloak)"
docker compose up -d

info "Attente de la disponibilité des services"
wait_healthy nutrition-postgres 120
wait_healthy nutrition-redis    60
# Keycloak importe le realm au premier démarrage : prévoir large
wait_healthy nutrition-keycloak 240

info "Application des migrations EF Core"
dotnet ef database update \
    --project src/NutritionApi.Infrastructure \
    --startup-project src/NutritionApi.Api \
    >/dev/null
success "Base migrée"

info "Chargement des données de développement"
# Le seed passe par le conteneur : pas besoin d'un client psql sur la machine hôte.
# ON_ERROR_STOP=1 fait échouer le script si une instruction SQL échoue.
docker exec -i nutrition-postgres \
    psql -U postgres -d nutrition_dev -v ON_ERROR_STOP=1 --quiet \
    < seed-dev.sql >/dev/null
success "Données de test chargées"

printf "\n"
success "Environnement de développement prêt"
cat <<'EOF'

  PostgreSQL   localhost:5445   (base nutrition_dev, postgres/postgres)
  Redis        localhost:6336
  Keycloak     http://localhost:8778   (console admin : admin/admin)

  Comptes de test — mot de passe « test » :
    test-user    rôle user,  abonnement Free
    test-pro     rôle user,  abonnement Pro
    test-admin   rôle admin, abonnement Free

  Lancer l'API :  dotnet run --project src/NutritionApi.Api
  Swagger      :  http://localhost:5099/swagger

EOF
