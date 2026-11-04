# 0074. The folders say what ships

**Status:** Accepted · **Date:** 2026-11-04 (Louis, reading the solution in Rider: "can we group this via function",
"can we make the folder structure mirror it", "this is only the puls config", "this would be like someone projects",
"only framework stuff should be in source right?", "if evals is framework it should move", "the folder structure must
match, these two items could be put in one ADR", "this is actually demos", "should be called demos") · **Recorded:**
2026-11-04

## Context

This is a framework that will ship as NuGet packages ([ADR 0073](0073-packs-reference-no-evals.md)), but the folders didn't
say so. `src/` held Puls's web app beside the engine, the eval harness sat outside it, and the solution's groups were one
map while the disk was another. Someone new couldn't tell what they would get by taking the framework.

Weighed: *separate folders in one solution* (taken); *two solutions in this repo*, a firmer wall, but the second still
needs project references until the packages exist; and *separate repos*, which is the end state (F44.4) and needs the
packages published first.

## Decision

- **The solution's groups and the folders on disk are the same map.** A project sits at the path its groups spell.
- **`src/` holds only the framework:** `engine/`, `hosts/`, `web/` and `evals/`, grouped by function.
- **`packs/` holds tools and nothing else;** what proves a pack sits apart in `packs-evals/`, so someone can take the tools
  without the evals, or not own them.
- **Anything that names a product is a demo of the framework, under `demos/`**, as someone's own project would be, by product and then by function, using
  the same names as `src/`: `demos/puls/{identity,evals,web,hosts}` and `demos/louis-agent/{web,hosts}`. So it
  can be lifted out as is, and a new product looks like these do.

`SolutionLayoutTests` holds all of it, project by project.

## Consequences

- **Framework still points at configuration in two places, and that is a debt with a name.** The API's csproj picks a web app
  with `-p:WebApp=puls`, and the hosts reference `host-packs`. CL29.1 inverts these, so a host takes its catalogue from
  outside.
- `louis-agent.web.shared` is framework by this rule but 13 of its 49 files mention Puls, which needs its own look.
- `tests/` stays one folder for now, and gets the same split next. When the framework moves to its own repo, its tests go with it.
- A demo holds its own `config/` and `docker/`, so a host is told where its config is (`CONFIG_DIRECTORY`). `prices.json` and `rates.json` are one copy per demo, held equal by a test.
- Moving a project changes paths in project files, Docker, docs and tests, once, and `git` sees renames. Open branches
  rebase on it.

## Links

[0010](0010-folders-by-concern.md) · [0027](0027-engine-identities-and-packs.md) · [0073](0073-packs-reference-no-evals.md) ·
[CL29 plan](../plans/CL29-packs-are-just-tools.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
