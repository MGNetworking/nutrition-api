#!/usr/bin/env bash
#
# Lance les tests d'intégration externe (niveau 3 — NTR-28).
#
#   ./scripts/test-integration.sh
#   ./scripts/test-integration.sh --no-build        # réutilise la compilation existante
#
# Monte PostgreSQL, Redis et Keycloak, attend qu'ils soient prêts, puis exécute les tests
# marqués Level=3.
#
# Réutilise la même pile que ./scripts/dev-up.sh — même docker-compose.yml, mêmes healthchecks,
# mêmes fonctions de lib.sh. Un seul environnement à connaître, en local comme en CI.
#
# Deux différences avec dev-up.sh :
#   - pas de seed-dev.sql : chaque test sème ses propres données
#   - pas de migrations ici : la fabrique de tests crée une base par exécution et l'y applique
#
# Idempotent : peut être relancé sur un environnement déjà démarré.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

cd "$REPO_ROOT"

require_docker

info "Démarrage des services (PostgreSQL, Redis, Keycloak)"
docker compose up -d

info "Attente de la disponibilité des services"
wait_healthy nutrition-postgres 120
wait_healthy nutrition-redis    60
# Keycloak importe le realm au premier démarrage : prévoir large
wait_healthy nutrition-keycloak 240

info "Exécution des tests de niveau 3"

# Le filtre porte sur le trait, pas sur le projet : un test de niveau 3 ajouté ailleurs par erreur
# serait tout de même exécuté ici, et resterait exclu de la CI unitaire.
dotnet test nutrition-api.slnx \
    --filter "Level=3" \
    --settings tests/coverage.runsettings \
    "$@"

printf "\n"
success "Tests de niveau 3 terminés"
