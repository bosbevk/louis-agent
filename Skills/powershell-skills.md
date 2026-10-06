# PowerShell Skills

How to execute PowerShell with the native `PowerShell*` tools. Use PowerShell for Windows and system administration tasks (services, processes, registry, event logs, file system bulk operations), working with objects/JSON/CSV, and calling REST APIs with `Invoke-RestMethod`. Prefer Python for heavy data processing or anything needing third-party libraries.

## Tools

| Tool | Use it to |
|------|-----------|
| `PowerShellRun` | Run an inline `script` **or** a workspace `.ps1` via `scriptPath` (never both), with optional `arguments`, `standardInput` and `timeoutSeconds`. Returns exit code, output and errors. |
| `PowerShellInfo` | Check whether pwsh 7 (Core) or Windows PowerShell 5.1 (Desktop) is in use, plus version and OS. |

Scripts run with the workspace root as the current directory, without profiles and non-interactively (`Read-Host`, `Get-Credential` and confirmation prompts fail instead of waiting).

## Writing scripts that work

- Start scripts with `$ErrorActionPreference = 'Stop'` so failures stop the script and set exit code 1, instead of printing a red error and carrying on.
- Return structured data as JSON: `... | ConvertTo-Json -Depth 5`. Default table formatting truncates columns and wraps lines.
- Use `-Confirm:$false` for cmdlets that would prompt, and `-Force` only when you mean it.
- Pass inputs as parameters (`param([string]$Path, [int]$Top = 10)`) and call with `arguments: "-Path data.csv -Top 5"`; read piped input with `[Console]::In.ReadToEnd()`.
- Use `exit <code>` to signal failure from a script; an uncaught error also exits with 1.

## Windows PowerShell 5.1 vs PowerShell 7

Check with `PowerShellInfo` before relying on newer features. In 5.1 there is **no** `&&`/`||`, ternary `?:`, `??`, `?.`, `ForEach-Object -Parallel` or `ConvertFrom-Json -AsHashtable`; use `if/else` and `-and`. `Invoke-WebRequest` in 5.1 may need `-UseBasicParsing`. In Docker (Linux) only pwsh exists and Windows-only cmdlets (registry, services, WMI/CIM on Windows) are unavailable.

## Workflows

### Inspect or change the system
1. Read first: run the `Get-*` command and check its output before any `Set-*`, `Stop-*`, `Remove-*`.
2. For changes, scope them narrowly (exact names/paths) and tell the user what you changed.

### Debug a failing script
1. Read the error record: the message, then `At <file>:<line> char:<n>` for the location (inline scripts show as `<script>`).
2. Common causes: a non-terminating error you expected to stop the script (add `-ErrorAction Stop`), a 5.1-only syntax gap (see above), or a path relative to the wrong folder (use `$PSScriptRoot` inside script files).
3. Fix and re-run until it succeeds.

## Skills in PowerShell

Skills can use PowerShell by fencing the execution block with ```powershell. Read skill arguments as `$env:SKILL_ARG_name` — never paste user values into the script.

## Rules

- Never run destructive commands (`Remove-Item -Recurse`, `Stop-Process`, `Format-*`, `Set-ExecutionPolicy`, registry deletes, service changes) unless the user explicitly asked for that change.
- Don't modify user profiles, environment variables at machine/user scope, or execution policy.
- Report the actual output; if the script failed or timed out, say so rather than guessing the result.
