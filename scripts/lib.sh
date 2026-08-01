#!/usr/bin/env bash
# Fonctions partagées par les scripts d'environnement.
# Ce fichier n'est pas exécutable directement : il est sourcé par les autres scripts.

set -euo pipefail

# Racine du dépôt, quel que soit le répertoire depuis lequel le script est appelé
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

# Couleurs désactivées si la sortie n'est pas un terminal (logs CI lisibles)
if [ -t 1 ]; then
    C_INFO='\033[0;36m'; C_OK='\033[0;32m'; C_WARN='\033[0;33m'; C_ERR='\033[0;31m'; C_OFF='\033[0m'
else
    C_INFO=''; C_OK=''; C_WARN=''; C_ERR=''; C_OFF=''
fi

info()    { printf "${C_INFO}==>${C_OFF} %s\n" "$*"; }
success() { printf "${C_OK}  ✓${C_OFF} %s\n" "$*"; }
warn()    { printf "${C_WARN}  !${C_OFF} %s\n" "$*"; }
fail()    { printf "${C_ERR}  ✗ %s${C_OFF}\n" "$*" >&2; exit 1; }

# Vérifie que le démon Docker répond — message explicite plutôt qu'une erreur brute
require_docker() {
    command -v docker >/dev/null 2>&1 \
        || fail "Docker n'est pas installé ou absent du PATH."

    docker info >/dev/null 2>&1 \
        || fail "Le démon Docker ne répond pas. Démarrer Docker Desktop puis relancer ce script."
}

# Vérifie que l'outil EF Core est disponible
require_dotnet_ef() {
    command -v dotnet >/dev/null 2>&1 \
        || fail "Le SDK .NET n'est pas installé ou absent du PATH."

    dotnet ef --version >/dev/null 2>&1 \
        || fail "L'outil dotnet-ef est absent. L'installer avec : dotnet tool install --global dotnet-ef"
}

# Attend qu'un conteneur passe healthy.
# Usage : wait_healthy <nom_conteneur> <timeout_secondes>
wait_healthy() {
    local container="$1"
    local timeout="${2:-120}"
    local elapsed=0
    local status

    while [ "$elapsed" -lt "$timeout" ]; do
        status="$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null || echo absent)"

        case "$status" in
            healthy)   success "$container est prêt"; return 0 ;;
            absent)    fail "Le conteneur $container n'existe pas." ;;
            unhealthy) fail "Le conteneur $container est unhealthy. Diagnostiquer avec : docker logs $container" ;;
        esac

        sleep 2
        elapsed=$((elapsed + 2))
    done

    fail "$container n'est pas devenu healthy en ${timeout}s. Diagnostiquer avec : docker logs $container"
}
