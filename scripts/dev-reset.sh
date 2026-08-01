#!/usr/bin/env bash
#
# Remet l'environnement de développement à zéro.
#
#   ./scripts/dev-reset.sh
#
# Supprime les données, puis reconstruit un environnement neuf : services,
# migrations et données de test. À utiliser quand la base locale est dans un
# état incohérent, ou pour repartir d'une installation propre.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

cd "$REPO_ROOT"

require_docker

warn "Toutes les données locales vont être supprimées"

"$REPO_ROOT/scripts/dev-down.sh" --volumes
printf "\n"
"$REPO_ROOT/scripts/dev-up.sh"
