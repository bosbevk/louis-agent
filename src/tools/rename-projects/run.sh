#!/usr/bin/env bash
# F167, run once: every louis-agent.* project, its folder, its docs and its pictures renamed to the name its code
# uses (louis-agent.core to LouisAgent.Core). Louis, 2026-11-21, of the rename tool: "Drop the tool", and of the
# names in the text: "3 just as a once off just too many to test". So these commands are the record of what ran,
# kept here, and they have no tests; the build and the whole unit suite proved the result.
#
#   bash src/tools/rename-projects/run.sh          # dry: writes names.tsv, moves.tsv and report.txt, changes nothing
#   bash src/tools/rename-projects/run.sh write    # the run: the text rewritten, then every git mv
#
# The text is rewritten before anything moves, so every path is still where git says it is. This folder and F167's
# own plan are left out of the rewrite, since they name the old projects on purpose.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
here=src/tools/rename-projects
mode=${1:-dry}

perl "$here/make-names.pl" > "$here/names.tsv"
perl "$here/make-moves.pl" "$here/names.tsv" > "$here/moves.tsv"
git grep -z -l -I -E 'louis-agent\.[a-z]' -- . ":(exclude)$here" ":(exclude)src/docs/plans/*F167-*" \
    | perl "$here/rename.pl" "$here/names.tsv" "$mode" > "$here/report.txt"
if [ "$mode" = write ]; then perl "$here/apply-moves.pl" "$here/moves.tsv"; fi
head -1 "$here/report.txt"
