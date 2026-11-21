#!/usr/bin/env bash
# Runs when a Claude Code session starts (.claude/settings.json, SessionStart).
#
# Louis, 2026-10-19: "when you make a commit, commit as https://github.com/bosbevk". His PC has that in the
# repo's own git config. A fresh clone does not, and a cloud thread's machine says the author is Claude. So
# where no author is set, or Claude is, this sets Louis in the repo's own config. Anyone else's own name is
# left as it is.
#
# What it prints is added to what Claude reads at the start of the session.

# The repo is two folders up from this file, wherever the session was started from.
repo="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$repo" || exit 0
git rev-parse --git-dir > /dev/null 2>&1 || exit 0

email="$(git config user.email || true)"
case "$email" in
    "" | *@anthropic.com)
        git config user.name "Louis Marais"
        git config user.email "84603591+bosbevk@users.noreply.github.com"
        ;;
esac

echo "Commits in this repo are authored as $(git config user.name) <$(git config user.email)>." \
    "No line in a commit or a pull request names Claude (/ship-it)."
