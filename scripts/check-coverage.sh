#!/usr/bin/env bash
#
# Vérifie la couverture par couche contre les seuils déclarés (NTR-132).
#
#   ./scripts/check-coverage.sh                          # rapport par défaut
#   ./scripts/check-coverage.sh chemin/vers/Cobertura.xml
#
# Les seuils ne sont écrits qu'à un seul endroit : le bloc <Thresholds> de
# tests/coverage.runsettings. Ce script les y lit, il ne les redéfinit pas.
#
# Pourquoi ce script existe (constaté le 2026-08-02) : ce bloc <Thresholds> était
# jusqu'ici décoratif. coverlet.collector ne le lit pas — seule l'intégration MSBuild
# de Coverlet gère les seuils. Vérifié en exécutant NutritionApi.Api.Tests, qui passait
# au vert avec une couverture très inférieure à son seuil déclaré. L'action de
# commentaire de PR ne bloquait pas davantage : son fail_below_threshold vaut false
# par défaut. Aucune barrière n'existait, alors que le README affirmait le contraire.
#
# Le rapport attendu est le rapport FUSIONNÉ (niveaux 1, 2 et 3 réunis). Mesurer une
# couche sur une partie seulement de ses tests donne un chiffre faux : Infrastructure
# tombe à 7,9 % quand on écarte le niveau 3, contre son vrai taux tous tests réunis.

source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

cd "$REPO_ROOT"

REPORT="${1:-coverage/report/Cobertura.xml}"
SETTINGS="tests/coverage.runsettings"

[[ -f "$REPORT" ]]   || fail "Rapport de couverture introuvable : $REPORT"
[[ -f "$SETTINGS" ]] || fail "Fichier de réglages introuvable : $SETTINGS"

thresholds="$(grep -oE '<Coverage Assembly="[^"]+" Line="[0-9]+" Branch="[0-9]+"' "$SETTINGS" || true)"
[[ -n "$thresholds" ]] || fail "Aucun seuil déclaré dans $SETTINGS — le bloc <Thresholds> est vide ou absent."

info "Contrôle des seuils de couverture"
printf "\n  %-28s %-18s %-18s\n" "Couche" "Lignes" "Branches"
printf "  %-28s %-18s %-18s\n" "----------------------------" "------------------" "------------------"

violations=0

while IFS= read -r decl; do
    assembly="$(sed -E 's/.*Assembly="([^"]+)".*/\1/' <<< "$decl")"
    min_line="$(sed -E 's/.*Line="([0-9]+)".*/\1/'   <<< "$decl")"
    min_branch="$(sed -E 's/.*Branch="([0-9]+)".*/\1/' <<< "$decl")"

    measured="$(grep -oE "<package name=\"${assembly}\" line-rate=\"[0-9.]+\" branch-rate=\"[0-9.]+\"" "$REPORT" | head -1 || true)"

    # Un assembly déclaré mais absent du rapport est une erreur, pas un succès :
    # il signale un renommage de projet, ou une exécution partielle des tests.
    if [[ -z "$measured" ]]; then
        printf "  %-28s %s\n" "$assembly" "absent du rapport"
        violations=$((violations + 1))
        continue
    fi

    line_rate="$(sed -E 's/.*line-rate="([0-9.]+)".*/\1/'     <<< "$measured")"
    branch_rate="$(sed -E 's/.*branch-rate="([0-9.]+)".*/\1/' <<< "$measured")"

    read -r line_pct branch_pct line_ok branch_ok <<< "$(
        awk -v l="$line_rate" -v b="$branch_rate" -v ml="$min_line" -v mb="$min_branch" \
            'BEGIN { printf "%.1f %.1f %d %d", l*100, b*100, (l*100 >= ml), (b*100 >= mb) }'
    )"

    [[ "$line_ok" -eq 1 ]]   && line_mark="OK"   || { line_mark="SOUS SEUIL";   violations=$((violations + 1)); }
    [[ "$branch_ok" -eq 1 ]] && branch_mark="OK" || { branch_mark="SOUS SEUIL"; violations=$((violations + 1)); }

    printf "  %-28s %5s%% / %3s%% %-6s %5s%% / %3s%% %-6s\n" \
        "$assembly" "$line_pct" "$min_line" "$line_mark" "$branch_pct" "$min_branch" "$branch_mark"
done <<< "$thresholds"

printf "\n"

if [[ "$violations" -gt 0 ]]; then
    fail "$violations seuil(s) non tenu(s). Les seuils sont déclarés dans $SETTINGS."
fi

success "Tous les seuils de couverture sont tenus"
