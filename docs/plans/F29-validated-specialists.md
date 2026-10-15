# F29 · Validated specialists: plan

> Approved 2026-10-15 · Feature: [F29 Validated specialists](../features/F29-validated-specialists.md) · Branch:
> `claude/project-thread-381vmw`, between F28's step 1 and its remaining steps (F28's last step touches the same
> checks)

## Context

Louis (2026-10-15): "we can actually disable all specialists and Puls can only boot with web, except cook, cause the
others have to go through the same process: testing, validation." Today a chat opens with `agents` only and the
router hands every kind of request to a role; the cook is the only role with a report shape, a relay rule and a
check.

## Design

What changes from the feature doc: nothing; the doc was written with this plan.

- **Reuse before building:** `AgentOptions`'s list parsing (as `AGENT_PEERS`), `AgentHost.PersonalityFile`'s
  existing refusal and `ErrorMessage.UnknownPersonality` (the gate makes a role that's off look unknown, with the
  enabled list as "available"), the API's existing 400 for a `PersonalityException`, the checks' existing
  `expected_role`. Nothing third-party. Considered: deleting the role files (loses the work, and the peer still
  needs the cook); a prompt-only switch (a slip in the router would still reach the researcher). Chosen: a setting
  the API enforces, and prompts that match it.
- **`PERSONALITIES_ENABLED`** (`AgentOptions.PersonalitiesEnabled`, an ordered set; empty: all):
  `AgentEngineFactory.AvailablePersonalities` filters by it; `AgentHost.PersonalityFile` refuses a name not on it,
  naming the enabled ones. `config/.env.example` and `docs/SETUP.md` document it; `docker-compose.puls.yml` sets
  `puls,puls-cook` on both containers.
- **The chat:** `ChatPage` opens a session with `agents` and `web` when both are available (`agents` alone when
  `web` isn't; every toolset when there's no peer, as today).
- **Prompts:** `default.md`'s table: the web row's role becomes "you, with `web`"; the files, git, code, dotnet,
  paymo, devops and skills rows say *not yet*; a line under the table says what *not yet* means. `agents-skills.md`'s
  roles table keeps the cook; the others move under *Waiting for validation*.
- **Checks:** `puls-check`'s `EXPECTED_ROLE` maps only `cook`; its delegated mode is `["agents", "web"]`; the
  "routed right" of a non-cook question is "no delegation". `puls-cook-check`'s `expected_role` is the same rule
  and its comments name `puls-web` as Puls as built.

## Steps

One commit each, with its tests; the review stop after each.

| Step | What | Check |
|---|---|---|
| 0 | Feature doc and this plan; TODO line; index rows | — |
| 1 | `PERSONALITIES_ENABLED` in options, the factory and the host; env example, setup doc, the Puls compose | factory tests first (filtered list; a name off the list refused before anything is built; empty means all) |
| 2 | The chat opens with `agents` and `web` | by hand (a web question answered by Puls itself) |
| 3 | Prompts: the router's role column and its *not yet* line; the agents guide's table | prompt tests first; golden files |
| 4 | Both checks' expectations | `--report` on the saved samples (unchanged scores) |

## Verification

- `docker compose … up -d --build`; `GET /personalities` on 5070 and 5071 lists `puls` and `puls-cook`.
- By hand: a web question (Puls answers itself, sources shown), a cooking question (the cook's card), a files question
  (Puls says it isn't available yet).
- A run of the delegation check from the portal (Louis's go; about R1): routed right 12/12 in the delegated mode.

## Approved during the build

(none yet)
