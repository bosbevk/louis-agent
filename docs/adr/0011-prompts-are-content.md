# 0011. Prompts are content: plain markdown skills, no provider class per toolset

**Status:** Accepted; file names amended by [0016](0016-skills-by-level.md), the disk override ended by
[0017](0017-built-in-prompts-embedded.md) · **Date:** 2026-10-11 · **Recorded:** 2026-10-14

## Context

The system prompt and the procedures the agent runs are text that changes often, and is tuned by reading it, not by
compiling it. When prompt text lives in C#, or a toolset's guide gets its own provider class, a wording change needs a
rebuild and a code review, and the same loading logic gets copied for every toolset.

## Decision

System prompt text and procedures live in `Skills/*.md`; C# only loads them (CL1's layout rule, 2026-10-11). Every skill
file we control is plain markdown read by `MarkdownSkillProvider`; a toolset's guide is its
`Skills/{toolset}-skills.md`, named by the catalogue's `GuideFile` (since F5, 2026-10-13). A provider class of its own
is only for a skill format we don't control.

## Consequences

- Wording changes without a rebuild: a file on disk overrides the copy embedded in core.
- Adding a toolset is a catalogue entry, a tool class and a guide, with no new loading code; `SkillFilesTests` fails if
  a provider class for one of our files appears.
- What the model is sent is pinned by `ModelRequestSnapshot.json`, so a prompt change is seen in review.

## Links

[Adding a toolset](../../louis-agent.core/CLAUDE.md#adding-a-toolset) ·
[Project layout](../../louis-agent.core/CLAUDE.md#project-layout) · [F5 plan](../plans/F05-toolset-profiles.md)
