# 0018. Built-in prompts are embedded, read only from the assembly

**Status:** Accepted · **Date:** 2026-10-16 · **Recorded:** 2026-10-16

## Context

The built-in prompts (the voices, the router, the specialists' roles, the toolset guides, the built-in function skills)
lived in the repo's `Skills/` and were embedded into core as linked files, and a copy on disk overrode the embedded
one, so wording could change with a restart (ADR 0011). Two costs showed during CL8: Rider listed every linked file
flat at core's root, with no folders, and a host read a prompt from one of several places (the skills folder, the
working directory and its parents, `/app`), so what the model was sent depended on which copy was found first. A stale
copy in a mounted folder would silently win.

Considered for reading them: `Microsoft.Extensions.FileProviders.Embedded`; keeping our `SkillFileSource` on
`GetManifestResourceStream` (no package, but folder paths become dotted names); `.resx` (built for short localized
strings, not markdown).

## Decision

We keep the built-in prompts in `louis-agent.core/Prompts/`, one folder per level (ADR 0017) beside `toolsets/`, embed
them with a manifest (`GenerateEmbeddedFilesManifest`), and read them only through `ManifestEmbeddedFileProvider`, by
their own paths. A file on disk never overrides one: a copy with a built-in's name is ignored, with a warning. The
skills folder (`SKILLS_DIRECTORY`, else the repo's `Skills/`, found by its README) holds only what the agent writes:
its function skills (`agent/functions/`) and its tools (`tools/`).

## Consequences

- What the model reads is fixed by the build: one source, no search order, and the golden file now pins the real
  router too.
- A wording change is a rebuild of the images that use core, not a restart; editing a mounted file no longer works.
- A personality or a guide can't be added on disk; it is a file in `Prompts/` and a rebuild.
- Core depends on `Microsoft.Extensions.FileProviders.Embedded` (MIT, Microsoft).
- ADR 0011's "wording changes without a rebuild" no longer holds; its decision (prompts are plain markdown, loaded by
  C#) stands.

## Links

[CL8 plan](../plans/CL8-skills-by-level.md) (*Approved during the build*, step 4) ·
[ADR 0011](0011-prompts-are-content.md) · [ADR 0017](0017-skills-by-level.md)
