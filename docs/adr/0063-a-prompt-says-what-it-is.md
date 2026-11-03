# 0063. A prompt says what it is

**Status:** Accepted · **Date:** 2026-11-03 (CL27, built unattended at Louis's word; the picks wait for his review
on the pull request) · **Recorded:** 2026-11-03

Numbered 0063 because 0062 is taken by F49.5's branch (*The studio writes what git keeps*), still open when this was
written.

## Context

A built-in prompt (a mode, a specialist's role, a minion, a toolset's guide, the principles) said what it was only
through its first heading and the folder it sat in ([ADR 0017](0017-skills-by-level.md)). One kind of file had more:
F33 gave a role a small header of its own, a `toolsets: shops?` line between two `---` lines, read by a hand-written
splitter (`RoleHeader`) that ignored every other key ([ADR 0025](0025-a-role-declares-its-toolsets.md)). Nothing else
could ask a file what it was, so the portal couldn't list the prompts, and a misspelled key loaded nothing, quietly.

The Agent Skills spec, which the function skills will follow in Phase 7, opens every `SKILL.md` with YAML front
matter: a `name` (the folder's own), a `description` (what it does and when to use it, at most 1024 characters) and a
`metadata` map for anything else. Louis's TODO line (2026-10-30) asked for that header on every role, mode, guide and
judge rubric, ours (`level`, `toolsets`) under `metadata`, read once by the skill loader and shown in the portal,
with the built-in prompts still embedded ([ADR 0018](0018-built-in-prompts-embedded.md)).

Weighed for the reader: YamlDotNet into a strict check (taken); `RoleHeader` grown by hand to nested keys (a YAML
subset by hand, which [ADR 0030](0030-build-or-adopt.md) says to adopt instead); JSON front matter (not the spec).
Weighed for strictness: an unknown key refuses the file (taken); unknown keys ignored, as `RoleHeader` did; the spec's
other optional keys allowed and ours strict (keys nothing reads).

## Decision

- **Every built-in prompt, and every judge rubric, opens with the front matter:**

  ```markdown
  ---
  name: puls-cook
  description: Puls's local cook. A meal the user can buy for and make today, with the recipe and a shopping list.
  metadata:
    level: specialist
    toolsets: shops?
  ---
  # Puls specialist: Local cook
  ```

  `name` is the file's own name without `.md`; `description` says what it is and when it is loaded, at most 1024
  characters; `metadata.level` is `agent`, `specialist` or `minion` for a mode, a role or a minion, and a guide or the
  principles name none; `metadata.toolsets` is the comma list a role always loads, a `?` loading only where the
  instance has it ([ADR 0025](0025-a-role-declares-its-toolsets.md)). Its values are text, as the spec's map of strings
  is.
- **One reader, and it is strict.** `PromptHeaders.Split` in core reads the YAML with YamlDotNet's node model and
  gives the header apart from the body. An unknown key, a name that isn't the file's, a level that isn't one, a
  missing or too long description and an unclosed fence each refuse the file, naming it and what to change
  ([ADR 0049](0049-nothing-is-compatible-before-a-sale.md): no lenient reader). `RoleHeader` is gone.
- **Read once.** `PromptCatalog` reads every embedded prompt's header on the first ask, after the host has registered
  its identities' and packs' prompts (Lazy Load), so one faulty header stops the host when it starts, not when a chat
  opens that role. The loader takes bodies from it, a session its role's toolsets (`SkillLoader.ToolsetsOf`), the
  portal its list.
- **The model and the judge get only the body.** No prompt the model reads carries a header (the prompt goldens did
  not move), and a judge is sent the rubric's body; the rubric's hash is the body's, so the cook's stays
  `438a8deb91f4` and every stored calibration still matches it.
- **A file with no header is read whole.** The agent's own function files on disk have none until Phase 7 gives them
  the same front matter as `SKILL.md` folders.
- **The level lives at the top of `Skills/`.** The header names a level and the skills' folders may use only the top
  (`ArchitectureTests`), so `RoleLevel` moved there from `Roles/`, and a role's toolsets are asked of `SkillLoader`
  (in `Hosting/`), not `RoleCatalog`.
- **The portal shows them.** `GET /v1/admin/prompts`, behind the admin key, lists every built-in prompt's path, name,
  description, level and toolsets; Puls's admin has a *Prompts* page under a *Build* section of its sidebar.

## Consequences

- A prompt can be asked what it is without being read by a model: the portal lists them, and a test holds that every
  built-in prompt has a header whose name is its file's and whose level is its folder's (`EveryPromptHasAHeaderTests`).
- Core carries one more package, YamlDotNet (MIT, the same version as the eval runner's).
- The Prompts page is one table sorted by path, not grouped by kind: the path already sorts the kinds apart, and a
  grouping would have put the folder names as literals in Puls ([ADR 0043](0043-repeated-names-are-enums.md)).
- A judge rubric isn't in the portal's list: it is an eval's content, in its folder beside the identity, not an
  embedded prompt. Its header is there for the studio and for a person reading the file.
- Phase 7's function skills take the same header, so the agent's own skills and the built-in prompts read alike.

## Links

TODO's *CL27 Prompt headers* · [0017](0017-skills-by-level.md) · [0018](0018-built-in-prompts-embedded.md) ·
[0025](0025-a-role-declares-its-toolsets.md) · [0030](0030-build-or-adopt.md) ·
[0049](0049-nothing-is-compatible-before-a-sale.md) · [write-up 07](../learning/07-context-engineering.md#a-prompt-says-what-it-is-cl27) ·
[louis-agent.core](../projects/louis-agent.core.md) · the Agent Skills specification · YamlDotNet

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
