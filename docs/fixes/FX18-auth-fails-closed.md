# FX18 · Without a key, the API and the MCP server over HTTP serve anyone who can reach them

> **Status:** done · **Area:** core, `Guardrails/HostAuth` (new); the API's and the MCP server's start-up and key
> check (`Program.cs` in both hosts) · **Found:** 2026-10-18, reading the code for
> [the platform plan](../plans/platform.md) (at `main` 7894953) · **Fixed:** 2026-10-19 ·
> **Planned:** the plan's Phase 1, one step, so no plan file of its own

## Symptom (found in the code; not seen in a run)

- Without `AGENT_API_KEY`, the API logged a warning and served `/sessions`, `/workspace`, `/git`, `/toolsets`, the
  roles routes and `/model` to anyone who could reach it, and its agent runs shell commands and writes files
  (`src/louis-agent.api/Program.cs:41-60` at `7894953`).
- The MCP server over HTTP (`MCP_TRANSPORT=http`) did the same without `MCP_API_KEY` or `AGENT_API_KEY`
  (`src/louis-agent.mcp-server/Program.cs:86-95`; `config/.env.example:86-88`).
- The settings said so: the API was open without the key (`config/.env.secrets.example:33-35`; `docs/SETUP.md:129-130`;
  `docker/docker-compose.yml:107`; `docker/docker-compose.puls.yml:47`).

## Cause

Both hosts failed open: a missing key was a warning at start-up, not a refusal, so a host started without one was open
by accident. The same fixed-time key check was written twice, once in each host (`src/louis-agent.api/Program.cs:228-234`;
`src/louis-agent.mcp-server/Program.cs:115-121`), so the two could drift. The admin gate already showed the other way:
with no key the portal is closed, and the rule is pure and tested without a web host
(`src/louis-agent.api/Services/AdminGate.cs:6-8`; `tests/louis-agent.api.tests/AdminGateTests.cs`).

## Fix

As designed in [the platform plan](../plans/platform.md), Phase 1 (*FX18 · Auth fails closed*):

- **One setting, `AGENT_AUTH`:** `required` (the default) or `off`, an enum (`Config/AuthMode`,
  [ADR 0024](../adr/0024-choices-are-enums.md)) parsed once with `SettingChoice`; an unknown value stops start-up
  naming the setting and its two values (`ErrorMessage.UnknownAuthMode`).
- **One pure rule in core, `Guardrails/HostAuth`,** which both hosts call: `AtStartup(mode, key)` is `Start` when a key
  is set, whatever the mode (so `off` never silences a key), `Refuse` with no key under `required`, and `StartOpen`
  with no key under `off`; `HasKey(x-api-key, authorization, expected)` is the fixed-time check both hosts had, so
  their `HasApiKey` copies are deleted. Core references no ASP.NET Core, so a host hands it the two header strings.
- **The hosts:** on `Refuse` the host throws `ErrorMessage.ApiKeyRequired` (*`AGENT_API_KEY is required: set it in
  config/.env.secrets, or set AGENT_AUTH=off to serve without a key on purpose.`*; the MCP server names
  `MCP_API_KEY (or AGENT_API_KEY)`) before it listens, the way `AgentHost.Build` already stops on a bad price table;
  on `StartOpen` its reworded warning says the auth is off on purpose; on `Start` the middleware checks every request,
  as before. The MCP server over stdio is untouched: a child process of its client, on the same machine.
- **Settings and docs** say the key is required: `config/.env.example` documents `AGENT_AUTH`;
  `config/.env.secrets.example`, `docs/SETUP.md`, both compose files' comments, the API's and MCP server's design docs,
  `docs/AGENT_COMMUNICATION.md`, the building-block view and `docs/CLAUDE.md` (*Quick start*, *Common errors*). No
  compose file sets `off`.
- **What changed from the plan's namespace map:** one more `ErrorMessage` member, `UnknownAuthMode`, for the value
  that names nothing; and `AGENT_AUTH` is parsed in `HostAuth.Mode`, not `AgentOptions`, because the MCP server gets
  only an engine from `AgentHost.BuildToolHost()` and has no options object. `AdminGate` keeps its own compare and
  header read; folding it onto `HostAuth` is a later tidy.
- **What it changes for Louis:** his local stacks need `AGENT_API_KEY` in `config/.env.secrets` (both compose files pass
  it in; `puls` already sends it to `puls-agent` as its peer key), or `AGENT_AUTH=off` in his `config/.env`; then
  `up -d`, since `restart` keeps the old environment. The checks and the orchestrator already send the key when set.

## Tests

`tests/louis-agent.core.tests/Guardrails/HostAuthTests.cs`, pure as `AdminGateTests` is, written first against stubs
and seen failing (24 cases, all `NotImplementedException`):

- `TheMode_IsRequiredUnlessOffIsSaid`: unset, empty and blank are `required`; `required`, `REQUIRED`, `off` and
  ` Off ` parse.
- `AnUnknownMode_NamesTheSettingAndItsValues`: `sometimes` and `1` throw, naming `AGENT_AUTH`, `required` and `off`.
- `AtStartup_RefusesWithoutAKey_UnlessOffIsSaid`: `(required, null)` and `(required, "  ")` refuse; `(off, null)`
  starts open; a key starts under either mode.
- `HasKey_ReadsXApiKeyThenBearer_AndComparesWhole`: `x-api-key` and `Authorization: Bearer` (either case) pass; a
  wrong key, an empty one, neither header, and one that differs only in length fail; `x-api-key` wins when both are
  present.

The golden files didn't change: no `ToolMessage` or prompt moved. `HostMessageEnumTests` covers the two reworded
warnings.

## Confirmed

By hand on 2026-10-19, free, each host run from a folder outside the repo (so no `config/.env.secrets` loads), the API on
an Ollama profile so no model key is needed:

| Host | Settings | Outcome |
|---|---|---|
| API | no key, no `AGENT_AUTH` | stops before listening: `Unhandled exception. System.InvalidOperationException: AGENT_API_KEY is required: set it in config/.env.secrets, or set AGENT_AUTH=off to serve without a key on purpose.` |
| API | `AGENT_AUTH=off`, no key | starts; `[WARN] AGENT_AUTH=off: no AGENT_API_KEY, so the API accepts unauthenticated requests, on purpose. …`; `GET /toolsets` 200 |
| API | `AGENT_API_KEY=k` | `/toolsets` 401 without a key and with a wrong one, 200 with `x-api-key: k` and with `Authorization: Bearer k`; `/health` 200 without; no warning |
| API | `AGENT_AUTH=sometimes` | stops: `Unknown AGENT_AUTH 'sometimes'. Use required or off.` |
| MCP server, HTTP | no key, no `AGENT_AUTH` | stops: `MCP_API_KEY (or AGENT_API_KEY) is required: …` |
| MCP server, HTTP | `AGENT_AUTH=off`, no key | starts with its warning; `initialize` 200 |
| MCP server, HTTP | `MCP_API_KEY=k` | 401 without the key, 200 with it |

## Lesson

Deny by default (OWASP Top 10:2021, A01): fail closed at start-up rather than at the first request, with one pure rule
tested without a host. A warning that a door is open is not a guard: the operator who never reads the log never
closes it, so the host has to refuse, and the one who wants it open says so in a setting that is never set in a
compose file. Skill map #15 (safety and security).

---
[Fixes](README.md) · Previous: [FX17](FX17-cook-labels-and-counts.md) · Next: [FX19](FX19-git-arguments-as-data.md)
