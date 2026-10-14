# Plan: CL5 Guides by folder (cleanup)

> **For:** the TODO's *Guides by folder* (cleanup) · **Branch:** `docs/CL5-guides-by-folder`, stacked on
> `docs/CL4-doc-types` (both earlier branches change `docs/CLAUDE.md` and aren't merged yet); its PR goes in after
> theirs · **Approved:** 2026-10-14

## Context

`CLAUDE.md` imports `docs/CLAUDE.md` with `@`, so the whole development guide loads into every Claude Code session.
`/context` measured it at 15.2k tokens; after the two PRs it is about 11.4k (28,391 characters at the measured ~2.5
characters per token). Every other doc costs nothing until it is opened. Much of the guide only matters in one folder:
how to add a tool, the layout rules and the message enums are needed only when `louis-agent.core` changes.

Claude Code loads a `CLAUDE.md` in a subfolder **only when it works on files in that folder**. The `@` import, by
contrast, always loads. So folder-specific how-tos can move next to the code they describe: the same progressive
disclosure as F5's toolset guides and CL4's single-type docs, applied to the guide Claude itself reads.

**This is a move, not a rewrite.** Every rule keeps its one-line form in the root's *Rules that always apply*, which
points to where the detail now is. No text the agent's model sees changes. Nothing paid runs.

## Design

| File | Holds | ~Tokens | Loaded |
|---|---|---|---|
| `docs/CLAUDE.md` (root) | Working with Louis; steps, branches, commits; rules that always apply; quick start; project tree; adding a skill or personality; running and debugging; known limitation 9; common errors; the tier and MCP notes; **a table of the folder guides** | ~5.5k | Every session |
| `louis-agent.core/CLAUDE.md` | Adding a C# tool, adding a toolset, project layout, messages and logging, building another agent on core, changing the LLM | ~4.6k | When working in core |
| `tests/CLAUDE.md` | Tests: commands, the golden files (bash and PowerShell), fakes, platform-neutral | ~0.9k | When working in tests |
| `src/CLAUDE.md` | The hosts' own message enums (from *Messages and logging*), adding a web theme | ~0.3k | When working on a host |

**Per session:** ~11.4k → ~5.5k tokens (about −50%), plus ~4.6k when core is touched, where those rules matter.

**Why there's no `Skills/CLAUDE.md`:**
- **The build would ship it.** `louis-agent.core.csproj` embeds every `Skills/*.md` into the product (line 33).
- **The agent would load it as a personality.** `Personalities.cs:61` lists every `.md` in `Skills/personalities/` as
  one.
- So the skill and personality how-tos (~0.8k) stay in the root guide.

**Rules that cross folders:** "no inline message text" applies to the hosts too, but `src/` work doesn't load core's
guide. Two things cover this: each such rule's one-liner stays in the root, and the hosts' part of the rule moves to
`src/CLAUDE.md`.

**Docs conventions:**
- Each folder guide is a **How-to**, opens with the CL4 opening line, and gets a row in `docs/README.md`.
- Cross-references such as "(*Tests*)" become links to the folder guide.
- `ArchitectureTests` reads only `*.cs`, so a `CLAUDE.md` in core is safe. Nothing in `src/` or `tests/` globs `*.md`.

**Not in this plan:**
- Project skills (`.claude/skills/`) for procedures. Revisit if a procedure grows.
- Splitting the human docs: AGENT_COMMUNICATION, ARCHITECTURE, TODO.

## Steps (each one commit; a review stop after each)

0. **The plan in the repo:** `docs/plans/CL5-guides-by-folder.md`, its row in the plans table, and the TODO line.
1. **`louis-agent.core/CLAUDE.md`:** the six core sections move there unchanged. The root gets the folder-guide table,
   and its references to those sections become links.
2. **`tests/CLAUDE.md`:** *Tests* moves there. Update the references in *Finishing a step*, *Adding a C# tool* and
   elsewhere.
3. **`src/CLAUDE.md`:** the hosts' message enums and adding a web theme.
4. **Index and finish:** rows in `docs/README.md` for the three guides; the TODO line moves to the CHANGELOG.

## Verification

- **Link check after every step:** the CL4 script from the scratchpad (rewrite it if it is gone), so `#anchors` into the
  moved sections are caught. Zero broken links.
- **No text lost:** each moved section is compared line by line with its new home, as CL4's benchmark move was.
- **Size:** the root guide's characters before and after, converted at 2.5 characters per token.
- **Lazy loading, checked by Louis in fresh sessions** (it can't be checked from inside a session):
  1. Start a new session and run `/context`. It should list `CLAUDE.md` and `docs/CLAUDE.md` only, with a smaller
     figure than 11.4k.
  2. Ask for a change in a file under `louis-agent.core/`. `/context` should then list `louis-agent.core/CLAUDE.md`.
  3. If a folder guide never appears, stop and rethink before step 2.
- **Nothing the model sees changes:** `git diff --stat docs/CL4-doc-types -- Skills/` is empty, and the
  `ModelRequestSnapshotTests|SkillFilesTests|ArchitectureTests` tests pass.
- No paid run.

## Concepts

- **Context engineering for the coding agent (#7):** a fixed context that holds only what every task needs, with the
  rest loaded where it applies.
- **Measure, don't assume (#13, #12):** `/context` before and after. The lazy loading is checked, not assumed.
