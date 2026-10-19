# FX18 · Without a key, the API and the MCP server over HTTP serve anyone who can reach them

> **Status:** open · **Area:** the API's and the MCP server's start-up and key check (`Program.cs` in both hosts) ·
> **Found:** 2026-10-18, reading the code for [the platform plan](../plans/platform.md) (at `main` 7894953) ·
> **Planned:** the plan's Phase 1, one step, so no plan file of its own

## Symptom (found in the code; not yet seen in a run)

- Without `AGENT_API_KEY`, the API logs a warning and serves `/sessions`, `/workspace`, `/git`, `/toolsets`, the roles
  routes and `/model` to anyone who can reach it, and its agent runs shell commands and writes files
  (`src/louis-agent.api/Program.cs:41-60`).
- The MCP server over HTTP (`MCP_TRANSPORT=http`) does the same without `MCP_API_KEY` or `AGENT_API_KEY`
  (`src/louis-agent.mcp-server/Program.cs:86-95`; `config/.env.example:86-88`).
- The settings say so: the API is open without the key (`config/.env.secrets.example:33-35`; `docs/SETUP.md:129-130`;
  `docker/docker-compose.yml:107`; `docker/docker-compose.puls.yml:47`).

## Cause

Both hosts fail open: a missing key is a warning at start-up, not a refusal, so a host started without one is open by
accident. The same fixed-time key check is written twice, once in each host (`src/louis-agent.api/Program.cs:228-234`;
`src/louis-agent.mcp-server/Program.cs:115-121`), so the two can drift. The admin gate already shows the other way: with
no key the portal is closed, and the rule is pure and tested without a web host
(`src/louis-agent.api/Services/AdminGate.cs:6-8`; `tests/louis-agent.api.tests/AdminGateTests.cs`).

## Fix (to do)

Designed in [the platform plan](../plans/platform.md), Phase 1 (*FX18 · Auth fails closed*): one setting, `AGENT_AUTH`
(`required`, the default, or `off`; an enum, [ADR 0024](../adr/0024-choices-are-enums.md)), and one pure rule in core,
`HostAuth` in `Guardrails/`, which both hosts call. With `required` and no key the host refuses to start, before it
listens; with `off` it starts and says the auth is off on purpose; a key that is set is always checked. The MCP server
over stdio needs no key.

## Tests (to do)

`tests/louis-agent.core.tests/Guardrails/HostAuthTests.cs`, pure as `AdminGateTests` is, written first and seen
failing (the plan's Phase 1, step 1).

## Confirm

By hand, free: the API started without a key stops with the error; with `AGENT_AUTH=off` it starts with the warning;
with a key, a request without it is a 401; the MCP server over HTTP behaves the same.

## Lesson

Deny by default (OWASP Top 10:2021, A01): fail closed at start-up rather than at the first request, with one pure rule
tested without a host. Skill map #15 (safety and security).

---
[Fixes](README.md) · Previous: [FX17](FX17-cook-labels-and-counts.md) · Next: [FX19](FX19-git-arguments-as-data.md)
