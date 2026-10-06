# Bash Skills

How to execute shell scripts with the native `Bash*` tools. Use bash for command-line pipelines (`grep`, `find`, `sed`, `awk`, `sort`, `xargs`), quick HTTP calls with `curl`, and Unix tooling inside Docker. Prefer a dedicated tool when one fits (`Git*`, `DotNet*`, workspace file tools), Python for real data processing, and PowerShell for Windows administration.

## Tools

| Tool | Use it to |
|------|-----------|
| `BashRun` | Run an inline `script` **or** a workspace `.sh` via `scriptPath` (never both), with `arguments` (as `$1 $2 …`), `standardInput` and `timeoutSeconds`. Strict mode (`set -euo pipefail`) is on unless `strict: false`. |
| `BashInfo` | Which bash is used, its version, and which commands exist (`git`, `curl`, `jq`, `python3`, …). Check before relying on a command. |

Scripts run from the workspace root. On Windows this is **Git Bash** (never WSL); in Docker it is Linux `/bin/bash`.

## Writing safe scripts

- Quote every expansion: `"$1"`, `"$file"`, `"$@"`. Unquoted variables split on spaces and expand globs.
- Pass user-supplied values as `arguments` and read them as `$1`, `$2` or `"$@"` — never paste them into the script text.
- Put `--` before user-supplied paths (`rm -- "$f"`, `grep -- "$pattern" file`) so a value starting with `-` isn't read as an option.
- Strict mode stops at the first failing command. For commands that may legitimately fail (e.g. `grep` with no match), use `|| true` or check `$?` explicitly.
- Under `nounset`, reference optional variables as `"${VAR:-default}"`.
- With `pipefail`, piping a long output into `head` can fail ("write error", exit 141) when `head` closes the pipe early. Sort/filter first and cut last (`... | sort -rn | sed -n '1,3p'`) or add `|| true` to that pipeline.
- Use `find ... -print0 | xargs -0` (or `while IFS= read -r line`) for file lists that may contain spaces.
- For JSON, use `jq` if `BashInfo` shows it; otherwise switch to `PythonRun`.

## Git Bash on Windows

- Paths look like `/c/Users/...`; Windows paths `C:/Users/...` (forward slashes) also work. Backslashes need quoting.
- No `apt`/package manager, and some tools differ from Linux (e.g. no `jq` by default, `sed -i` and `find` are GNU but `ps`/`top` are limited).
- Scripts with Windows line endings (CRLF) fail with `$'\r': command not found` — save `.sh` files with LF endings.
- Windows `.exe` programs on PATH (`dotnet`, `git`, `python`) can be called directly.

## Workflows

### Find or transform files
1. Explore read-only first (`find`, `grep -rn`, `wc -l`) and show what will change.
2. Apply the change with a precise command (`sed -i 's/old/new/' -- "$file"`), then verify with `grep` or `git diff`.

### Debug a failing script
1. Read the error and exit code; with strict mode the failing command is the last one that ran.
2. Re-run with `set -x` at the top to trace each command, find the cause, fix, and remove the trace.

## Rules

- Never run destructive commands (`rm -rf`, `chmod -R`, `chown -R`, `git reset --hard`, `git push --force`, writes outside the workspace) unless the user explicitly asked for that change.
- Don't install software, change shell profiles, or modify system configuration.
- Report the actual output; if the script failed or timed out, say so.
