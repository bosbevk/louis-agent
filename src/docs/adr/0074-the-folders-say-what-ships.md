# 0074. The folders say what ships

**Status:** Accepted · **Date:** 2026-11-04 (Louis, reading the solution in Rider: "can we group this via function",
"can we make the folder structure mirror it", "this is only the puls config", "this would be like someone projects",
"only framework stuff should be in source right?", "if evals is framework it should move", "the folder structure must
match, these two items could be put in one ADR", "this is actually demos", "should be called demos") · **Recorded:**
2026-11-04

**Updated (CL32, 2026-11-07):** the folders this ADR names are now one area, `packs/tools/`, `packs/evals/`, `packs/tests/` and `packs/web/` ([ADR 0090](0090-the-packs-are-one-area.md)); the rule is unchanged.

## Context

This is a framework that will ship as NuGet packages ([ADR 0073](0073-packs-reference-no-evals.md)), but the folders didn't
say so. `src/` held Puls's web app beside the engine, the eval harness sat outside it, and the solution's groups were one
map while the disk was another. Someone new couldn't tell what they would get by taking the framework.

Weighed: *separate folders in one solution* (taken); *two solutions in this repo*, a firmer wall, but the second still
needs project references until the packages exist; and *separate repos*, which is the end state (F44.4) and needs the
packages published first.

## Decision

**Framework and implementation are split, and this comes first.** For every folder, file and reference, ask who owns it:
the framework's code, or what one running product configures, writes or deploys. Framework code can write a thing and the
thing still belongs to the demo: core writes skill files, and the running demo owns them.

- **The solution's groups and the folders on disk are the same map.** Anything the solution lists sits at the path its
  groups spell, so the tree a person reads in Rider is the tree on disk.
- **`src/` holds only the framework:** `engine/`, `hosts/`, `web/`, `evals/`, `tools/`, `tests/` and `docs/`, grouped by function.
  The one exception is the framework's own website, `src/site/`, built on the framework like any site, which ships no
  package and which nothing else in `src/` references ([ADR 0111](0111-the-frameworks-website-lives-in-src-site.md)).
- **`packs/` holds tools and nothing else.** What proves a pack sits apart in `packs-evals/`, so someone can take the tools
  without the evals or not own them, and its tests in `packs-tests/`.
- **Anything that names a product is a demo of the framework, under `demos/`**, as someone's own project would be, by
  product and then by function, using the same names as `src/`. It can be lifted out as is, and a new product looks like
  these do:

  ```
  demos/puls/         identity/ evals/ web/ hosts/ tests/ docs/ config/ docker/ skills/ deploy/
  demos/louis-agent/  web/ hosts/ tests/ docs/ config/ docker/ skills/
  ```

- **A test sits on the side it proves.** `src/tests/` holds only tests of the framework alone.
- **A doc lives with what it is about, an ADR included.** `src/docs/` is the framework's, `demos/{demo}/docs/` one demo's,
  `packs/docs/` the packs'. So each demo has its own Setup, and the framework's says only what its code reads. ADR numbers
  are shared across all of them, and the framework's ADR index lists every one.
- **What a run leaves behind sits in its demo too** (`data/`, `logs/`, and secrets), on disk and git-ignored, outside the
  solution. `.env` is listed in the solution; secrets are not.
- **A framework host is told where a demo's things are** (`CONFIG_DIRECTORY`, `SKILLS_DIRECTORY`), or finds them searching
  upward from where it runs.
- **Top-level folders are lowercase**, like everything else at that level.
- **A host ships as a library, and each demo writes the small program naming its packs** (Composition Root). So a pack is
  switched on by a line in that program, and a setting does it only where a demo's program chooses to read one, as Puls's
  `host-packs` reads `IDENTITIES` and `PACKS`.

`SolutionLayoutTests` holds the map, the top-level list and what each demo carries.

## Consequences

- **Framework still points at implementation in three places, and each is a debt with a name.** The API's csproj picks a
  web app with `-p:WebApp=puls`. The hosts reference `host-packs`. And the API's tests name Puls and the packs, so they sit
  in `demos/puls/tests/` and the framework ships without API tests until the hosts are inverted (CL29.1).
- `louis-agent.web` is framework by this rule, and [ADR 0084](0084-every-site-is-built-on-the-frameworks-web.md) now holds it to naming no site.
- `prices.json` and `rates.json` are one copy per demo, held equal by a test, as are the env templates' shared settings by
  hand.
- Inside a project, a folder is still PascalCase, because the namespace is the folder path (the orchestrator's `Skills/`).
- `deploy/` is Puls's because only Puls is deployed today. A second deployed demo brings its own.
- Moving `docs/` under `src/` changed every `<seealso>` link in the code once. That was weighed against keeping a top-level
  `docs/`, and taken so it only has to be done once and `src/` really is the whole framework.
- The `.env.example` templates are still copies of each other, not split per demo.
- Moving a project changes paths in project files, Docker, docs and tests, once, and `git` sees renames. Open branches
  rebase on it.

## Links

[0010](0010-folders-by-concern.md) · [0027](0027-engine-identities-and-packs.md) · [0073](0073-packs-reference-no-evals.md) ·
[0076](0076-core-ships-no-agent.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
