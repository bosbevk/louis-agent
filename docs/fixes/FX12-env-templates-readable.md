# FX12 · `.env` templates (`.env.example`) can be read

> **Status:** done · **Area:** core, `Guardrails/SensitiveFiles` (file tools and the API's file and git views) · **Found:** 2026-10-13

## Symptom

In the [Puls delegation check with roles](../../samples/sample-output-puls-check-roles/README.md), question files-2
("which settings in config/.env.example are about the LLM?") couldn't be answered in either run. The specialist got
*"Reading this sensitive file is not allowed."* The baseline agent got the same refusal and pieced the settings
together from the code and the README instead, which is slower and less complete.

## Cause

`SensitiveFiles.IsSensitive` blocks every file whose name starts with `.env`. That rule is right for `.env` and
`.env.secrets`, but it also catches the templates (`.env.example`, `.env.secrets.example`). Those are committed to the
repository on purpose and document every setting, with no values that matter.

## Fix

A name that starts with `.env` is sensitive unless it ends with `.example`, `.sample` or `.template`, the usual names
for a committed template (case ignored). Everything else stays blocked:
- the real files (`.env`, `.env.secrets`);
- the LLM profiles (`.env.anthropic`, `.env.ollama`), which are not templates and could hold a key;
- a local copy of a template (`.env.example.local`), because it doesn't *end* with the template ending.

The same rule covers reading, writing, searching, the web app's file view and git diffs, since all of them ask
`SensitiveFiles`.

**The risk, accepted:** someone could put a real secret in an `.env.example`. That file would be committed to git
anyway, so the agent reading it exposes nothing that isn't already in the repository. The agent can also now *write*
a template, which is ordinary editing. It still can't copy a real `.env` into one, because the source is blocked.

## Tests

- `SensitiveFilesTests`: templates are allowed (`config/.env.example`, `config/.env.secrets.example`, `.env.SAMPLE`,
  `.env.template`). Still blocked: `config/.env.anthropic` and `.env.example.local`.
- `WorkspaceToolsTests.ReadFile_AllowsAnEnvTemplate`: `ReadWorkspaceFile` reads `config/.env.example`, and
  `config/.env` next to it stays blocked.
- `files-skills.md` says templates can be read. The golden file changed for that line.

## Confirmed

Not yet. This needs the next run of the delegation check (a paid run): files-2 should list the settings from
`config/.env.example` itself.

## Lesson

A guardrail that blocks too much has a cost too: the agent works around it, guesses, or gives up. Block by what a
file holds, not by what its name looks like, and test both the blocked and the allowed side. Skill 15 (safety and
security) in the [goal](../GOAL.md).
