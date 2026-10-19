# FX20 · A skill file the agent writes is loaded at once, for every later session, with no person in between

> **Status:** done · **Area:** core, skill authoring (`Agent/Tools/SkillTools`, `AgentEngine`'s `/approve`, `/reject`
> and `/tools`) · **Found:** 2026-10-18, reading the code for [the platform plan](../plans/platform.md) (at `main`
> 7894953) · **Fixed:** 2026-10-19 · **Planned:** the plan's Phase 1, one step, so no plan file of its own

## Symptom (found in the code; the documented design, not a crash)

`CreateSkillFile` saved `Skills/agent/functions/{name}.md` and loaded it at once, for every later session
(`louis-agent.core/Agent/Tools/SkillTools.cs:149-168,201-205` at `7894953`). A skill's `## Skill:` sections carry an
`Execution:` fence that `ExecuteSkill` runs as bash, PowerShell or Python (`SkillTools.cs:140`), and `ExecuteSkill` is
registered in every engine (`AgentEngine.cs:141`). So text the model wrote once, perhaps steered by a page it read,
became code every later session could run.

## Cause

It was the documented design: the tool's description (`SkillTools.cs:149-168`), `docs/CLAUDE.md`'s *Adding a skill*,
`docs/architecture/06-runtime-view.md:19-20` and `Prompts/toolsets/python.md:37`. Agent-built tools already wait for a
person: they wait in a `pending` folder for `/approve`, and that folder is git-ignored
(`louis-agent.core/Agent/Tools/AgentBuiltTools.cs:12-13,30-32,142-154`; `.gitignore:50-51`). Skill files had no such
step, and `/approve` and `/reject` reached agent-built tools only (`AgentEngine.cs:303-317`). This fix changes
documented behaviour on purpose.

## Fix

As designed in [the platform plan](../plans/platform.md), Phase 1 (*FX20 · Skill files wait for approval*), mirroring
agent-built tools:

- **`CreateSkillFile` writes to `Skills/agent/functions/pending/{name}.md`** (`SkillFiles.PendingFunctionsFolder`).
  The file is active in the engine that wrote it, as a pending tool is, through what was already there: the engine's
  own list of files it wrote, which `ReloadSkills` adds to the loaded set. Its reply says it is pending and that the
  user should type `/approve {name}` (or `/reject {name}`); its description says the same. An approved or pending file
  of the name needs `overwrite`; with it, the new version waits in `pending/` while the approved one stays for other
  sessions (the engine that wrote it runs the new one: the composite provider's later file wins by skill name; its
  guidance text is in that engine's prompt twice until approval, noted).
- **Later sessions load only approved files**, with no change: discovery reads the top of `agent/functions/` only
  (`AgentHost.DiscoverSkillFiles`), so `pending/` is never read.
- **`/approve {name}`** moves the file up over the approved one, with **no reload**: this engine already runs it, and
  the entry in its own list follows the move, so a later reload finds it at its new path, once. **`/reject {name}`**
  deletes it and, if this engine had it, reloads so the skill is gone here. **`/tools`** lists pending skill files
  beside pending tools, saying whether this engine runs each. In `AgentEngine.HandleUserCommand` a name is routed to
  the tool when a tool of that name is pending, else to the skill file; a name that is both is refused, naming both
  files, and nothing is done. The "no pending tool" replies now say "tool or skill file".
- **`.gitignore`** gains `Skills/agent/functions/pending/`, as it had `Skills/tools/pending/`.
- **Docs and prompts:** `docs/CLAUDE.md` (*Adding a skill*), `docs/architecture/06-runtime-view.md`, the core design
  doc, the CLI's help line, `Prompts/toolsets/python.md` and `self-extension.md` say a written skill waits for
  `/approve`.
- **Known limitation 9 still holds:** `CreateSkillFile` reloads the skills, which rewrites `history[0]`, as it did
  before, and so does `/reject` of a file this engine runs; `/approve` reloads nothing. FX4 (Phase 7) removes it.
- **Worth knowing:** in the API every chat with the same toolsets, role and level shares one engine (F5), so a pending
  skill file is active in every chat on that engine until it is approved or rejected, as a pending tool is.

## Tests

`tests/louis-agent.core.tests/Agent/SkillAuthoringTests.cs`, extended: a second engine on the same temp skills folder
is "a later session". Written first and seen failing (eight red: the file landed in the top folder, a later engine
loaded it, `/approve greeting` said *No pending tool named 'greeting'*, `/tools` listed nothing):

- `CreateSkillFile_SavesLoadsAndMakesSkillExecutable` (changed): the file is under `pending/`, not approved, the reply
  names `/approve greeting`, and the skill runs in this engine.
- `APendingSkillFile_IsntLoadedByALaterSession`: a new engine's prompt lacks the guidance and `Greet` is not found.
- `Approve_MovesTheFile_ALaterSessionSeesIt_AndThisOneKeepsItWithoutAReload`: moved; a new engine runs it; this one
  still does, and after its own `ReloadSkills` the guidance is in its prompt once.
- `Reject_DeletesTheFile_AndUnloadsItHere`; `Approve_OfAnUnknownName_SaysSo` (both commands).
- `AName_ThatIsBothAPendingTool_AndAPendingSkillFile_IsRefused`: both files named, nothing moved.
- `Overwrite_KeepsTheApprovedVersion_UntilTheNewOneIsApproved`: pending `hi`, approved `hello`; this engine says `hi`,
  a new one `hello`; after `/approve`, a new one says `hi`.
- `Tools_ListsPendingSkillFiles`: the file this engine wrote as *active here* (the review of PR #68: in the API "here"
  is the engine, which chats share, so the label says neither "session" nor "engine"), one dropped in `pending/` by an
  earlier session as *not loaded*.
- `Approve_AndReject_TakeTheNameInAnyCase` (from the review): `CreateSkillFile` lowercases the name, so the commands do
  too; `/approve Greeting` finds `greeting.md`. Green on Windows before the fix, since its file system ignores case,
  which is why the review caught it and the test alone wouldn't have.

The golden files changed on purpose and their diff was reviewed: `CreateSkillFile`'s description and its `name` and
`overwrite` parameters, `SkillFileSaved`, and the two guide lines in the system prompt.

## Confirmed

By the tests: a new engine on the same folder doesn't see a pending skill file until `/approve`, and sees the new
version of an overwritten one only then.

## Lesson

Memory poisoning (OWASP Top 10 for Agentic Applications, ASI06): what the agent writes for later sessions is untrusted
until a person approves it, however reasonable it reads, because the model that wrote it may have been reading an
attacker's page. The gate costs nothing new: agent-built tools already had it, and the engine's "files I wrote" list
already kept a file alive for one session; the fix was to stop the file reaching every other session on its own.
Skill map #15 (safety and security) and #16 (human in the loop).

---
[Fixes](README.md) · Previous: [FX19](FX19-git-arguments-as-data.md)
