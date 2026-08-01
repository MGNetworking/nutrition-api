#!/usr/bin/env bash
#
# Démarre la stack Docker complète, API conteneurisée comprise.
#
#   ./scripts/docker-up.sh              démarre (build si l'image est absente)
#   ./scripts/docker-up.sh --rebuild    force la reconstruction des images
#
# Contrairement à dev-up.sh, l'API tourne ici dans un conteneur et charge
# appsettings.Docker.json : elle joint les services par leur nom de service.
#
# Les migrations sont appliquées par un conteneur one-shot qui s'arrête ensuite ;
# l'API ne démarre qu'après sa réussite.
#
# Pour arrêter : ./scripts/dev-down.sh

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

cd "$REPO_ROOT"

REBUILD=false

for arg in "$@"; do
    case "$arg" in
        --rebuild)
            REBUILD=true
            ;;
        --help|-h)
            cat <<'EOF'
Démarre la stack Docker complète, API conteneurisée comprise.

  ./scripts/docker-up.sh              démarre (build si l'image est absente)
  ./scripts/docker-up.sh --rebuild    force la reconstruction des images

Pour arrêter : ./scripts/dev-down.sh
EOF
            exit 0
            ;;
        *)
            fail "Option inconnue : $arg (options disponibles : --rebuild, --help)"
            ;;
    esac
done

require_docker

if [ "$REBUILD" = true ]; then
    info "Reconstruction des images (sans cache)"
    docker compose --profile full build --no-cache
else
    info "Construction des images si nécessaire"
    docker compose --profile full build
fi

info "Démarrage de la stack complète"
# Compose applique la chaîne : postgres healthy → migrations terminé → api
docker compose --profile full up -d

info "Vérification de l'exécution des migrations"
migration_exit="$(docker inspect --format='{{.State.ExitCode}}' nutrition-migrations 2>/dev/null || echo absent)"
if [ "$migration_exit" != "0" ]; then
    fail "Le conteneur de migrations a échoué (code $migration_exit). Diagnostiquer : docker logs nutrition-migrations"
fi
success "Migrations appliquées"

info "Chargement des données de développement"
docker exec -i nutrition-postgres \
    psql -U postgres -d nutrition_dev -v ON_ERROR_STOP=1 --quiet \
    < seed-dev.sql >/dev/null
success "Données de test chargées"

printf "\n"
success "Stack Docker complète démarrée"
cat <<'EOF'

  API          http://localhost:5100          (conteneurisée, profil Docker)
  Swagger      http://localhost:5100/swagger
  Keycloak     http://localhost:8778          (console admin : admin/admin)
  PostgreSQL   localhost:5445
  Redis        localhost:6336

  Logs de l'API :  docker logs -f nutrition-api
  Arrêter        :  ./scripts/dev-down.sh

EOF
