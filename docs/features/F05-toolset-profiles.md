# F5 · Toolset profiles: send only the tools a session needs

> **Status:** planned · **Milestone:** M2 · **Depends on:** F1
>
> **Spec:** [Optimisation §C](../specs/RESPONSE_OPTIMISATION.md)
>
> **In the TODO:** tick **F5 Toolset profiles** under *Now* ([TODO](../../TODO.md))

Every engine registers every toolset whose keys are set: in the demo, 108 tools (68,162 characters of definitions)
including 35 Paymo and DevOps tools a code-fixing container never uses. Their guidance in the system prompt is sent too
(`Skills/default.md` alone spends ~4,500 of its 12,434 characters on DevOps and Paymo tools and workflows).

F5 lets a **session** choose the toolsets it needs when it opens. Nothing loads at start-up. A session's toolsets are
fixed for its lifetime, so its prefix stays cacheable. A general agent can also open a **sub-session** on another agent
with the toolsets a task needs, and get the result back.

## Decisions

- **Per session, not per instance.** Toolsets are chosen with `POST /sessions` and fixed for that session. Changing them
  mid-chat is out of scope: it breaks the cached prefix and the Haiku 5.5 history rule.
- **Nothing at start-up.** The `AGENT_TOOLSETS` instance setting is dropped. The engine is built per profile, on demand.
- **Default:** a session created without `toolsets` gets `skills` only. Clients that need tools (the web app) send
  their own list.
- **Sub-sessions are one level deep.** A sub-session cannot open its own sub-sessions.

## User stories

**F5-S1 — Choose toolsets when a session opens.** As a *client*, I want to name the toolsets a session loads, so that it
only pays for the tools it can use.
- *Given* `POST /sessions` with `{ "toolsets": ["files","git","dotnet","skills"] }` *then* only those tools are
  registered, plus `ExecuteSkill`, and `GET /sessions/{id}` returns the list.
- *Given* no `toolsets` *then* the session gets `skills` only.
- *Given* an unknown name *then* the response is `400` with the valid names.

**F5-S2 — Guidance follows the tools.** As an *operator*, I want the system prompt to leave out guides for tools a
session doesn't have, so that the prompt shrinks with the toolset and the model isn't told about tools it lacks.
- *Given* `python` isn't in the session's toolsets *then* `python-skills.md` isn't in its system prompt; *given* `paymo`
  and `devops` aren't loaded *then* no Paymo or DevOps sections appear.

**F5-S3 — Discover toolsets cheaply.** As a *general agent*, I want to list the toolsets and what each does, without
loading their tool definitions, so that I can choose the right one.
- *Then* `GET /toolsets` returns each name, a one-line description, and whether it is available (its keys are set). It
  never returns tool definitions.

**F5-S4 — The demo uses a coding profile.** As an *operator* of the orchestrator demo, I want its fix sessions to load
only coding tools, so that every fix sends a smaller prefix.
- *Then* the orchestrator opens its sessions with `["files","git","dotnet","skills"]`; the benchmark's fixed prefix
  shrinks by ≥ 25% and 10/10 fixes are still confirmed.

**F5-S5 — A general agent opens a specialist session.** As a *general agent*, I want to start a session on another agent
with named toolsets, send it a task, and get the result back, so that tools are only loaded where a task needs them.
- *Then* `StartSubSession(agent, toolsets, message)` opens a session on the named peer (`AGENT_PEERS`), sends the
  message, returns the final text and a short usage line, and deletes the sub-session, even on failure.
- *Given* more than three sub-sessions in one turn, or one that runs past five minutes *then* the call fails with an
  error the parent can handle.
- *Given* a sub-session asks for the `agents` toolset *then* the API refuses it (`400`).

**F5-S6 — Stays cacheable.** As an *operator*, I want to know a smaller prefix is still cached, so that F4's savings hold.
- *Then* the session log line gives the prefix size estimate; F1 records show `cache_read` > 0 from the second request in
  the same session (the model's cache minimum: 512 tokens on Haiku 5.5; [F15](F15-model-limits.md) records it per model
  later). Each toolset profile has its own cache prefix.

## Design

| Toolset | Tools | Guide in the system prompt |
|---|---|---|
| `files` | `WorkspaceTools` | `default.md` § Workspace & File Management |
| `git` | `GitTools` | `default.md` § Git Tools, workflows |
| `dotnet` | `DotNetTools` | `dotnet-skills.md` |
| `python` / `powershell` / `bash` | the script tools | `python-` / `powershell-` / `bash-skills.md` |
| `web` | `WebTools` | `web-skills.md` |
| `skills` | `CreateSkillFile`, `ReloadSkills`, `CreateTool`, `ListAgentTools` (+ approved agent-built tools) | `self-extension-skills.md` |
| `paymo` | `PaymoTools` (key required) | `paymo-skills.md` (moved out of `default.md`) |
| `devops` | `DevOpsTools` (key, organisation and project required) | `devops-skills.md` (moved out of `default.md`) |
| `agents` | `AgentPeerTools`: `StartSubSession` (`AGENT_PEERS` required) | `agents-skills.md` |

`ExecuteSkill` is always registered.

### Components

- **Toolset catalogue** (new `louis-agent.core/Agent/ToolsetCatalog.cs`): each name maps to a description, required
  settings, a factory for its tool object, and its guide file.
- **Engine per profile** (new `louis-agent.core/Agent/AgentEngineFactory.cs`): built from `AgentHostContext`. Shares the
  chat client, usage sink, mapper, display, thinking and options. Caches one `AgentEngine` per sorted toolset list, up
  to a cap (16).
- **`AgentEngine`**: when `toolsets` is passed and includes `skills`, also registers the self-extension tools. The
  built-in path is unchanged.
- **`AgentHost.LoadSkills(agent, toolsets)`**: always includes personality and the rules in `default.md`; includes a
  guide only when its toolset is requested; `SkillReloader` captures the same profile.
- **Sessions** (`AgentSessions`, `AgentSession`): each session stores its engine. `/messages` and `GET /sessions/{id}`
  read it from the session, not from the singleton.
- **`AgentPeerTools`**: calls the peer's `POST /sessions`, `POST /messages` (reads the SSE stream to the end) and
  `DELETE /sessions/{id}`. Auth uses `AGENT_API_KEY` from config, never the model's arguments. A session created with
  `depth > 0` may not use the `agents` toolset.

## Implementation steps

1. **Read first.** Confirm the request model (`CreateSessionRequest`), `AgentOptions`, the tool constructors, and
   `AgentBuiltTools.LoadApprovedTools`. *Done when:* each signature is noted.
2. **Spec.** Commit this spec change before code.
3. **Toolset catalogue.** *Test:* names unique; unknown name lists valid names; missing key marks the entry unavailable
   without failing.
4. **Self-extension in the toolsets path.** *Test:* `[]` gives only `ExecuteSkill`; `["files","skills"]` gives workspace
   tools, `CreateTool` and `ExecuteSkill`, with no git or shell tools.
5. **Split `default.md`** into the Paymo and DevOps skill files, with today's content unchanged. *Test:*
   `DefaultSkillsProviderTests` updated; with `louis` each section appears exactly once.
6. **Profile-aware `LoadSkills`.** *Test:* for `["files","git"]` the system prompt has no Python, PowerShell, bash, web,
   Paymo or DevOps sections.
7. **Engine factory.** *Test:* the same list returns the same engine; different lists give different tools; invalid
   names throw before anything is created.
8. **Sessions own their engine.** `POST /sessions` takes `toolsets` (400 on unknown names); `/messages` and
   `GET /sessions/{id}` use the session's engine. *Test:* a `["files"]` session has no git tools; two sessions with
   different lists don't share tools; `GET /sessions/{id}` returns the list.
9. **`GET /toolsets`.** *Test:* lists names, descriptions and availability; no tool definitions.
10. **`AgentPeerTools.StartSubSession`.** *Test:* unknown peer fails without a network call; the child is deleted on
    success and on failure; `depth > 0` with `agents` gets `400`; the fourth call in a turn is refused; a timeout
    returns an error the parent can handle.
11. **Usage.** A sub-session's records carry the parent turn's id. Check the `JsonlUsageSink` format first.
    *Test:* a parent turn with one child writes both records with the same parent id.
12. **Startup log and config.** Log the session's toolsets, tool count and prefix size estimate. Add `AGENT_PEERS` to
    `config/.env.example`; document it in SETUP.
13. **Demo.** The orchestrator opens fix sessions with the coding profile; in `docker-compose.demo.yml` the specialist
    and general instances share an image. Benchmark: prefix ≥ 25% smaller, cache still hits, 10/10 fixes confirmed.
    Record prefix size and cost per fix against M2's F4 run.
14. **Docs.** SETUP (`AGENT_PEERS`, sessions), ARCHITECTURE (sessions and skills sections), samples README.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Benchmark: prefix ≥ 25% smaller, cache still hits, 10/10 fixes confirmed; numbers recorded. Delegation is kept as the
  default only for tasks where the ledger shows it is cheaper.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F5** in the TODO, set *Status*
  to done here and in the features table, and note it in the spec's implementation map.

## Out of scope

- Loading a toolset in the middle of a session (breaks the cache and the Haiku 5.5 rule).
- Detecting the language to pick toolsets automatically. The client or the general agent chooses.
- Sub-sessions on other machines, or auth beyond `AGENT_API_KEY`.
- A router that picks the specialist automatically.

## Risks

- **Cache per profile:** each profile has its own prefix and first-request cache write. Keep the number of profiles small.
- **Default change:** a client that expects tools without sending a list loses them. The web app must send its toolsets.
- **Engine count:** engines are cached but never disposed; the cap bounds memory.
- **No API tests exist:** steps 8 to 10 need new tests, or they'll only be checked by hand.

---
[Features](README.md) · Previous: [F4 Prompt caching](F04-prompt-caching.md) · Next: [F6 Estimates](F06-estimates.md)
