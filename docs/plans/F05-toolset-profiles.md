# F5 · Per-session toolsets and sub-sessions: implementation plan

**Goal:** no toolsets load at startup. A session loads the toolsets it asks for when it opens. A general agent can start a specialist session with the tools a task needs and get the result back.

**Target repo:** `bosbevk/louis-agent` (not this workspace).
**Depends on:** F1 (usage ledger), F4 (prompt caching).

## What the source shows (why the design looks like this)

1. `AgentSessions` holds one shared `AgentEngine`. Every session gets the same tools. `POST /sessions/{id}/messages` and `GET /sessions/{id}` take the singleton engine from DI.
2. `AgentEngine` already accepts a `toolsets` list. When given, it registers only those objects' public methods plus `ExecuteSkill`. The orchestrator uses this path. Each engine has its own `ToolRegistry`, so one engine per toolset profile works without changing the registry.
3. The `toolsets` path does not register `CreateSkillFile`, `ReloadSkills`, `CreateTool`, or `ListAgentTools`. Those exist only in the built-in path.
4. Skills are instance-wide. `AgentHost.LoadSkills(agent)` always adds personality, default, dotnet, python, powershell, bash, web, and self-extension guides.

## Design

- **Toolset catalogue:** one class maps each name to a description, required settings, a factory for its tool object, and its guide file.
- **Engine per profile:** an `AgentEngineFactory`, built from `AgentHostContext`, shares the chat client, usage sink, mapper, display, thinking, and options. It caches one engine per sorted toolset list.
- **Session owns its engine:** `AgentSession` stores its engine. History, streaming, and usage read from it, not from the singleton.
- **Peer tool:** `StartSubSession` is a public method on an `AgentPeerTools` object. Adding that object to a profile exposes the tool.

## Steps

### Step 0 · Decisions and spec
- Update `docs/features/F05-toolset-profiles.md`: toolsets are fixed for the session's lifetime, not the instance's. Drop `AGENT_TOOLSETS` as a startup setting.
- Decide the default for a request with no `toolsets`. The web app sends no body, so this decides whether it keeps tools. Suggested: `skills` only, and the web app sends its own list.
- Commit the spec change before any code.
- **Done when:** the spec change is committed and reviewed.

### Step 1 · Read what isn't yet verified
Read before coding: `src/louis-agent.api/Messages/` (`CreateSessionRequest`), `Config/AgentOptions.cs`, `WebSearchProviderFactory`, the `PythonTools` / `PowerShellTools` / `BashTools` constructors, `AgentBuiltTools.LoadApprovedTools`, and `AgentEngine.HandleUserCommand`. Note each signature and its visibility.
- **Done when:** the notes list each constructor and visibility.

### Step 2 · Toolset catalogue
New file: `louis-agent.core/Agent/ToolsetCatalog.cs`.
- Entries: `files` (WorkspaceTools), `git` (GitTools), `dotnet` (DotNetTools), `python`, `powershell`, `bash`, `web` (WebTools, with the search provider), `paymo` (needs key), `devops` (needs key, organisation, project), `skills` (see step 3), `agents` (AgentPeerTools, see step 8).
- Each entry: name, one-line description, required settings, factory, guide file.
- `ExecuteSkill` is always added.
- **Tests:** names are unique; an unknown name lists the valid names; a missing key marks the entry unavailable without failing startup.

### Step 3 · Self-extension in the toolsets path
In `AgentEngine.cs`, when `toolsets` is given and includes `skills`, also register `CreateSkillFile`, `ReloadSkills`, `CreateTool`, and `ListAgentTools`. Pass this through an explicit argument or flag, not by reaching into private fields. Leave the built-in path unchanged.
- **Tests:** `[]` gives only `ExecuteSkill`; `["files","skills"]` gives workspace tools, `CreateTool`, and `ExecuteSkill`, with no git or shell tools.

### Step 4 · Profile-aware skills
In `AgentHost.LoadSkills`, change the signature to `LoadSkills(agent, toolsets)`.
- Always include personality and the rules part of `default.md`.
- Include a guide only when its toolset is requested. Map python, powershell, bash, web, self-extension, and dotnet to their toolsets.
- Move the Paymo and DevOps sections out of `default.md` into `paymo-skills.md` and `devops-skills.md`. Keep the content unchanged.
- The `SkillReloader` closure captures the same profile, so `ReloadSkills` reloads the right guides.
- **Tests:** for `["files","git"]` the system prompt has no Python, PowerShell, bash, web, Paymo, or DevOps sections. With the full list, each section appears exactly once.

### Step 5 · Engine factory
New file: `louis-agent.core/Agent/AgentEngineFactory.cs`.
- Built from `AgentHostContext`. Holds the chat client, usage sink and mapper, `Display`, `Thinking`, `SupportsTools`, and `AgentOptions`.
- `GetOrCreate(IReadOnlyList<string> toolsets)`: sort and de-duplicate the list, return the cached engine or build one with the catalogue's objects and a profile-specific skill provider. Set `Display` and `Thinking` the way `AgentHost.Build` does.
- Thread-safe, using `ConcurrentDictionary.GetOrAdd`.
- Cap the cache (for example 16 profiles) so callers can't create unbounded engines.
- **Tests:** the same list returns the same engine; different lists give different tool sets; invalid names throw before anything is created.

### Step 6 · Sessions own their engine
In `AgentSessions.cs` and `Program.cs`:
- `AgentSession` takes and exposes its `AgentEngine`. `AgentSessions.Create(tags, engine)` uses `engine.NewHistory()`.
- `POST /sessions`: parse `toolsets` from `CreateSessionRequest`, validate names (400 with the valid list), get the engine from the factory, create the session.
- `POST /sessions/{id}/messages` and `GET /sessions/{id}`: use `session.Engine`. Remove the `AgentEngine` parameter from those handlers.
- `/prices`, `/rates`, `/git`, and `/workspace` are unchanged.
- **Tests:** a session with `["files"]` has no git tools; an unknown name returns 400; two sessions with different lists don't share tools; `GET /sessions/{id}` returns its toolsets.

### Step 7 · Catalogue endpoint
`GET /toolsets` returns names, descriptions, and availability flags. It never returns tool definitions.

### Step 8 · Sub-session tool
New class `AgentPeerTools`:
- `StartSubSession(string agent, string[] toolsets, string message)` calls `POST /sessions` on the named peer with the toolsets and a `depth` value, sends the message, reads the SSE stream to the end, and returns the final text plus a short usage line.
- In a `finally` block, `DELETE /sessions/{id}`, even on failure.
- Peers come from config: `AGENT_PEERS=name=url`. Auth uses `AGENT_API_KEY` from config, never from the model's arguments.
- Limits in code, not in the prompt: at most 3 sub-sessions per parent turn; 5-minute timeout per sub-session; the API rejects the `agents` toolset when `depth > 0`, so a sub-session can't start its own sub-sessions.
- Only the summary goes back to the parent.
- **Tests:** an unknown peer fails without a network call; the child is deleted on success and on failure; a depth-1 session asking for `agents` gets 400; the fourth call in a turn is refused; a timeout returns an error the parent can handle.

### Step 9 · Usage
Child usage must link to the parent turn. Add a `Parent` field to `UsageTags`, or a `depth` and parent id on the session. Check the `JsonlUsageSink` format first, since the ledger format is part of the record.
- **Test:** a parent turn with one child writes both records, and both share the parent id.

### Step 10 · Config and compose
- Add `AGENT_PEERS` to `config/.env.example` and `docs/SETUP.md`. Remove `AGENT_TOOLSETS` from the plan.
- Compose: the web agent gets `AGENT_PEERS` pointing at the specialist service. Bind the specialist to `127.0.0.1` on the host.
- The web app sends its toolsets when it creates a session, or relies on the default from step 0.

### Step 11 · Benchmark
Run ten to twenty tasks two ways: the general agent alone, and with delegation. Compare total cost per task from the ledger, including children, with caching on. Record prefix sizes and cache reads per profile.

### Step 12 · Docs and finish
Update `ARCHITECTURE.md` (sessions and skills sections), `SETUP.md`, and the samples README. Tick F5 in the TODO and the features table.

## Risks

- **Cache per profile:** each profile has its own prefix and its own first-request cache write. Keep the number of profiles small.
- **Default toolsets:** moving the default from "everything" to "nothing" breaks any client that expects tools without sending a list, including the web app until it's updated.
- **Engine count:** engines are cached but never disposed. The cap in step 5 bounds memory.
- **No API tests exist:** steps 6 to 8 need new tests, or they'll be checked by hand only.

## Approved during the build (2026-10-13)

- **The default engine has no built-in tools.** With no `toolsets`, the engine registers only `ExecuteSkill` and the
  self-extension tools (`CreateSkillFile`, `ReloadSkills`, `CreateTool`, `ListAgentTools`); the old tail (Workspace,
  Git, DotNet, Python, PowerShell, Bash, Web, Paymo, DevOps) is gone. Tests that need a toolset pass it.
- **DevOps keeps its guide.** `devops-skills.md`, `DevOpsSkillsProvider` and `DevOpsTools` stay; the catalogue's `devops`
  entry keeps `GuideFile: "devops-skills.md"` (Louis, 2026-10-13: "devops-skills must stay").
- **Paymo's guide is the native tools section.** `paymo-skills.md` lists the `PaymoTools` methods and workflows; its
  placeholder `## Skill:` sections are gone.
- **Step 4b · Built-in guides ship as embedded resources** (after step 4, before step 5):
  - `louis-agent.core` embeds `Skills/*.md` (`<EmbeddedResource>` with a `LogicalName` of the file name), and a new
    `Skills/SkillFileSource` reads a guide by name: a file of that name in the skills directory first (an override,
    editable without a rebuild), else the embedded copy.
  - What is embedded: `personality.md`, `default.md` and each toolset's guide (`ToolsetCatalog.GuideFile`). What stays
    on disk only: files the agent writes (`CreateSkillFile`), custom `AGENT_FUNCTION` files and agent-built tools
    (`Skills/tools/`); `ReloadSkills` re-reads disk and embedded alike.
  - Why: the guides always match the code that ships with them (step 5 loads a guide with its toolset), no upward
    search for a `Skills/` folder is needed at start-up, images don't depend on copying `Skills/`, and the golden file
    can't drift with the working copy.
  - **Tests:** with an empty skills directory every catalogue guide still loads (from the assembly); a file of the
    same name on disk wins; a name that is neither on disk nor embedded is reported as missing, as today; the model
    request snapshot is unchanged.

## Open decisions

1. Default toolsets for a session with no `toolsets` (suggest `skills` only).
2. Whether the general agent and the specialist run as two containers from the same image (suggest yes).
3. Maximum sub-sessions per turn and timeout (suggest 3 and 5 minutes).
