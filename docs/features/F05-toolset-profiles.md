# F5 · Toolset profiles: send only the tools a session needs

> **Status:** planned · **Milestone:** M2 · **Depends on:** F1
>
> **Spec:** [Optimisation §C](../specs/RESPONSE_OPTIMISATION.md)
>
> **In the TODO:** tick **F5 Toolset profiles** under *Now* ([TODO](../../TODO.md))

Every engine registers every toolset whose keys are set: in the demo, 108 tools (68,162 characters of definitions)
including 35 Paymo and DevOps tools a code-fixing container never uses. Their guidance in the system prompt is sent too
(`Skills/default.md` alone spends ~4,500 of its 12,434 characters on DevOps and Paymo tools and workflows). F5 lets an
instance load only what it needs.

## User stories

**F5-S1 — Choose the toolsets.** As an *operator*, I want to list the toolsets an instance loads, so that it only pays
for the tools it can use.
- *Given* `AGENT_TOOLSETS=files,git,dotnet,skills` *then* only those tools are registered (plus `ExecuteSkill`), and the
  startup log lists the loaded toolsets and the tool count.
- *Given* `AGENT_TOOLSETS` unset *then* behaviour is as today: everything whose keys are set.
- *Given* an unknown name *then* start-up fails with the list of valid names.

**F5-S2 — Guidance follows the tools.** As an *operator*, I want the system prompt to leave out guides for tools that
aren't loaded, so that the prompt shrinks with the toolset and the model isn't told about tools it doesn't have.
- *Given* `python` isn't loaded *then* `python-skills.md` isn't in the system prompt; *given* `paymo` and `devops`
  aren't loaded *then* no Paymo or DevOps sections appear.

**F5-S3 — The demo uses a coding profile.** As an *operator* of the orchestrator demo, I want `demo-api` to load only
coding tools, so that every fix sends a smaller prefix.
- *Then* `docker-compose.demo.yml` sets `AGENT_TOOLSETS=files,git,dotnet,skills` on `demo-api`; the benchmark's fixed
  prefix shrinks by ≥ 25% and 10/10 fixes are still confirmed.

**F5-S4 — Stays cacheable.** As an *operator*, I want to know a smaller prefix is still cached, so that F4's savings hold.
- *Then* the startup log prints the prefix size estimate; F1 records show `cache_read` > 0 from the second request (the
  Haiku 4.5 minimum is 4,096 tokens).

## Design

| Toolset | Tools | Guide in the system prompt |
|---|---|---|
| `files` | `WorkspaceTools` | `default.md` § Workspace & File Management |
| `git` | `GitTools` | `default.md` § Git Tools, workflows |
| `dotnet` | `DotNetTools` | `dotnet-skills.md` |
| `python` / `powershell` / `bash` | the script tools | `python-` / `powershell-` / `bash-skills.md` |
| `web` | `WebTools` | `web-skills.md` |
| `skills` | `CreateSkillFile`, `ReloadSkills`, `CreateTool`, `ListAgentTools` (+ approved agent-built tools) | `self-extension-skills.md` |
| `paymo` | `PaymoTools` (key required) | Paymo sections, moved out of `default.md` into `paymo-skills.md` |
| `devops` | `DevOpsTools` (key and project required) | DevOps sections, moved out of `default.md` into `devops-skills.md` |

`ExecuteSkill` is always registered (the orchestrator's `toolsets:` path already relies on that).

Changes:

- **`AgentOptions.Toolsets`** from `AGENT_TOOLSETS` (null = all available).
- **`AgentEngine` constructor:** each `AddPublicMethodsAsTools(...)` guarded by its toolset; key checks unchanged.
- **`AgentHost.LoadSkills`:** always-loaded guides become "loaded when their toolset is"; `default.md` is split so its
  tool sections are per toolset — either separate files (`default.md` keeps rules and workflows only) or headings the
  loader filters. Prefer separate files: simpler to load and to read.
- **Fixed for the instance's lifetime** (never per request — that would break the cache).

## Implementation steps

1. **Option parsing.** *Test:* names parsed and validated; unset = all.
2. **Guard registration.** *Test:* with `files,git` only, the tool list contains Workspace/Git tools and `ExecuteSkill`
   and nothing else; the existing "unique tool names" test passes for every profile.
3. **Split `default.md`'s DevOps and Paymo sections** into their skill files, keeping today's content when both are
   loaded. *Test:* `DefaultSkillsProviderTests` updated; with `louis` everything is still present exactly once.
4. **Guides follow toolsets** in `LoadSkills`. *Test:* the system prompt for `files,git,dotnet,skills` has no Python,
   PowerShell, bash, web, Paymo or DevOps guidance.
5. **Startup log** of toolsets, tool count and prefix size estimate.
6. **Demo profile** in `docker-compose.demo.yml`; benchmark run; record prefix size and cost per fix against M2's F4 run.
7. **Docs:** SETUP (setting), ARCHITECTURE (skills section), samples README.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Benchmark: prefix ≥ 25% smaller, cache still hits, 10/10 fixes confirmed; numbers recorded.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F5** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map.

---
[Features](README.md) · Previous: [F4 Prompt caching](F04-prompt-caching.md) · Next: [F6 Estimates](F06-estimates.md)
