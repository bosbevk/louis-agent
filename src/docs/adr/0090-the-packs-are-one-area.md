# 0090. The packs are one area, shaped like the others

**Status:** Accepted · **Date:** 2026-11-07 (Louis: "can't we just move this under packs in its own folder like
packs/evals?"; "A, do CL32 first then CL31") · **Recorded:** 2026-11-07

## Context

ADR 0086 kept a pack's tests, evals and web views apart from its tools, so someone can take a pack's tools without
them, and the first cut put each kind at the root of the repository: `packs/`, `packs-evals/`, `packs-tests/`,
`packs-web/`, with `packs/docs/` inside the first. `src/` and each demo already do it the other way: one root with
the same inner names (`engine/`, `hosts/`, `web/`, `evals/`, `tests/`, `docs/`). Four roots for one area is noise in
the solution tree, and each new kind would have been a fifth.

## Decision

The packs are one area, `packs/`, with the same inner names as `src/` and a demo: `tools/` holds the packs (what
ships: a pack is a folder someone can take), `evals/` what proves a pack, `tests/` a pack's own tests, `web/` a
pack's own screens, `docs/` their docs. The rules of ADR 0073 and 0086 stand unchanged on the new prefixes: a pack
references no eval, no web and no test; a pack's web references the framework's web and its pack; only the runner
references the evals. The solution tree is the disk (`SolutionLayoutTests`), the whole area is under the size rule
(`FileSizeTests`), and `ArchitectureTests` and `PackWebBoundaryTests` scope a pack to `packs/tools/` and its checks
to `packs/evals/`.

Weighed and not taken: only the three siblings under `packs/`, beside the pack projects (a folder of two kinds);
leaving it.

## Consequences

- A path that named `packs/louis-agent.pack.{x}` names `packs/tools/louis-agent.pack.{x}`; `packs-evals/`,
  `packs-tests/` and `packs-web/` are `packs/evals/`, `packs/tests/` and `packs/web/`. ADRs 0073, 0074 and 0086 say
  the old names where they were written; this one says the new.
- A pack, its tests, evals and web views are one level deeper, so their references to `src/` gained a level.
- A new kind of thing beside the packs is a folder under `packs/`, never a new root.

## Links

[ADR 0074](0074-the-folders-say-what-ships.md) · [ADR 0086](0086-a-packs-screens-are-described.md) ·
[ADR 0073](0073-packs-reference-no-evals.md) · [Build a pack](../guides/build-a-pack.md)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
