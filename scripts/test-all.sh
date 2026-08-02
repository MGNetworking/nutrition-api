#!/usr/bin/env bash
#
# Rejoue localement ce que fait ci-pr.yml, dans le même ordre et avec les mêmes réglages.
#
#   ./scripts/test-all.sh                  # les trois niveaux, couverture fusionnée, seuils
#   ./scripts/test-all.sh --no-integration # niveaux 1 et 2 seulement — boucle rapide
#   ./scripts/test-all.sh --no-build       # réutilise la compilation existante
#
# Pourquoi ce script existe : la CI enchaîne trois jobs — niveaux 1 et 2, niveau 3, puis fusion
# des rapports et contrôle des seuils. Reproduire cet enchaînement à la main demande six commandes
# et deux chemins de rapports à ne pas confondre. Une divergence entre ce qu'on lance chez soi et
# ce que lance la CI se paie en allers-retours sur une pull request.
#
# Les sorties restent là où le SDK les écrit, c'est-à-dire là où un IDE va les chercher :
#
#   tests/<Projet>/TestResults/*.trx                    résultats de l'exécution
#   tests/<Projet>/TestResults/<guid>/coverage.*.xml    couverture brute, deux formats
#   coverage/report/Cobertura.xml                       rapport fusionné des trois niveaux
#
# Le format .trx est celui que produit « exécuter tous les tests » depuis Visual Studio ou Rider :
# il s'ouvre dans l'IDE, avec le détail par test.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

cd "$REPO_ROOT"

integration=true
passthrough=()

for argument in "$@"; do
    case "$argument" in
        --no-integration) integration=false ;;
        *) passthrough+=("$argument") ;;
    esac
done

# Les exécutions précédentes sont effacées : sans cela, ReportGenerator fusionnerait aussi
# d'anciens rapports et publierait une couverture qui ne correspond à aucun état du code.
info "Nettoyage des résultats précédents"
rm -rf tests/*/TestResults coverage/report
success "TestResults et coverage/report vidés"

# ── Niveaux 1 et 2 ────────────────────────────────────────────────────────────
info "Tests de niveaux 1 et 2"

dotnet test nutrition-api.slnx \
    --configuration Release \
    --settings tests/coverage.runsettings \
    --filter "Level!=3" \
    --logger "trx" \
    --collect:"XPlat Code Coverage" \
    "${passthrough[@]}"

# ── Niveau 3 ──────────────────────────────────────────────────────────────────
if [[ "$integration" == true ]]; then
    info "Tests de niveau 3"

    # Le script voisin monte la pile docker-compose et attend ses healthchecks. Les arguments
    # lui sont transmis tels quels jusqu'à dotnet test.
    #
    # --configuration Release n'est pas décoratif : sans lui, le niveau 3 se compile en Debug
    # tandis que les niveaux 1 et 2 sont en Release. ReportGenerator fusionne alors deux rapports
    # produits contre des assemblages différents, et la couverture obtenue ne décrit aucun binaire
    # réel. Le contrôle des seuils passe malgré tout — un faux positif, exactement ce que cette
    # barrière est censée empêcher. La CI, elle, compile tout en Release.
    ./scripts/test-integration.sh \
        --configuration Release \
        --logger "trx" \
        --collect:"XPlat Code Coverage" \
        "${passthrough[@]}"
else
    warn "Niveau 3 ignoré — la couverture publiée ne décrira que les niveaux 1 et 2"
fi

# ── Fusion et seuils ──────────────────────────────────────────────────────────
info "Fusion des rapports de couverture"

command -v reportgenerator >/dev/null 2>&1 \
    || dotnet tool install -g dotnet-reportgenerator-globaltool >/dev/null 2>&1

export PATH="$PATH:$HOME/.dotnet/tools"

reportgenerator \
    "-reports:tests/**/TestResults/**/coverage.cobertura.xml" \
    "-targetdir:coverage/report" \
    -reporttypes:"Cobertura;Html" >/dev/null

success "Rapport fusionné : coverage/report/Cobertura.xml"
success "Rapport lisible  : coverage/report/index.html"

./scripts/check-coverage.sh coverage/report/Cobertura.xml
