#!/usr/bin/env bash
#
# Arrête l'environnement de développement local.
#
#   ./scripts/dev-down.sh              arrête les services, conserve les données
#   ./scripts/dev-down.sh --volumes    arrête les services et supprime les données
#
# Sans --volumes, la base retrouve son contenu au prochain dev-up.sh.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

cd "$REPO_ROOT"

PURGE_VOLUMES=false

for arg in "$@"; do
    case "$arg" in
        --volumes|-v)
            PURGE_VOLUMES=true
            ;;
        --help|-h)
            cat <<'EOF'
Arrête l'environnement de développement local.

  ./scripts/dev-down.sh              arrête les services, conserve les données
  ./scripts/dev-down.sh --volumes    arrête les services et supprime les données

Sans --volumes, la base retrouve son contenu au prochain dev-up.sh.
EOF
            exit 0
            ;;
        *)
            fail "Option inconnue : $arg (options disponibles : --volumes, --help)"
            ;;
    esac
done

require_docker

# --profile full élargit la portée aux services de ce profil (api, migrations).
# Sans lui, `docker compose down` les laisserait en cours d'exécution.
# Préciser un profil à l'arrêt ne démarre rien.
if [ "$PURGE_VOLUMES" = true ]; then
    info "Arrêt des services et suppression des volumes"
    docker compose --profile full down --volumes
    success "Services arrêtés, données supprimées"
    warn "Le prochain dev-up.sh repartira d'une base vierge (migrations + seed rejoués)"
else
    info "Arrêt des services"
    docker compose --profile full down
    success "Services arrêtés, données conservées"
fi
