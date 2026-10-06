# Python Skills

How to execute Python with the native `Python*` tools. Use Python for data wrangling, calculations, parsing files (CSV/JSON/XML), quick prototypes and scripts — anything where running real code beats reasoning about the answer.

## Tools

| Tool | Use it to |
|------|-----------|
| `PythonRun` | Run inline `code` **or** a workspace `scriptPath` (never both), with optional `arguments`, `standardInput` and `timeoutSeconds`. Returns exit code, output and traceback. |
| `PythonInstallPackages` | `pip install` packages (e.g. `requests pandas==2.2.2`) into the agent's own virtual environment. |
| `PythonInfo` | Interpreter path/version, the virtual environment location and installed packages. |

Code runs with the workspace root as the current directory, so relative paths like `data/input.csv` resolve inside the workspace.

## Workflows

### Answer a question by computing it
1. Write a short, self-contained snippet and run it with `PythonRun` (`code`). `print()` everything you need — only printed output comes back.
2. Report the printed result, not what you expected it to be. If you round or summarise, say so.

### Use a third-party package
1. Try the standard library first (`json`, `csv`, `re`, `statistics`, `datetime`, `pathlib`, `urllib`).
2. If a package is really needed, `PythonInstallPackages` it once, then `PythonRun`. A `ModuleNotFoundError` means it isn't installed in the agent's environment yet.

### Write a reusable script
1. Save it with `WriteWorkspaceFile` (e.g. `scripts/report.py`), reading inputs from `sys.argv` or stdin rather than hard-coding them.
2. Run it with `PythonRun` (`scriptPath`, `arguments`) and fix any errors until it succeeds.
3. If the user wants it as a repeatable skill, wrap it with `CreateSkillFile` (see below).

### Debug a failing script
1. Read the traceback bottom-up: the last line is the exception, the frame just above it is where it happened (inline snippets show as `<snippet>`).
2. Read the relevant code, fix the cause (not the symptom), and re-run to confirm.
3. For unclear failures, add temporary `print(..., file=sys.stderr)` diagnostics, run again, then remove them.

## Turning a procedure into a skill

`CreateSkillFile` saves a `<name>-skills.md` file to the skills folder and loads it immediately; use `ExecuteSkill` to run it. Each skill's execution block runs in bash from the workspace root, so call the interpreter there (`python3` in Docker; on Windows the interpreter shown by `PythonInfo`). Read skill arguments only as `"${SKILL_ARG_name}"` (quoted) — never paste user values into the command. Use `ReloadSkills` after editing a skills file by hand.

## Rules

- Never claim a result you didn't print and see. If the run failed or timed out, say so.
- Keep long-running or networked code to a sensible `timeoutSeconds`; a timeout usually means an infinite loop, a blocking `input()` without `standardInput`, or a hung request.
- Don't write outside the workspace or delete files unless the user asked for it.
- Only install packages the task needs, and mention any you installed.
- If `PythonRun` reports Python isn't installed, tell the user how to install it instead of retrying.
