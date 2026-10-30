# 0058. An eval is a folder of files, beside the identity or pack it proves

**Status:** Accepted · **Date:** 2026-10-30 (Louis's picks on the F49.2 plan's cards and during its build) ·
**Recorded:** 2026-10-30

## Context

Until F49.2 an eval was a C# class (`CookEval`, `RoutingEval`) in an eval set project
(`evals/louis-agent.evals.puls`), listed by an `IEvalSet` the runner knew by an enum. The class built its checks by
hand, read its dataset from a folder beside it, embedded its judge's prompt and said what a turn's facts were. So a
new eval, or a changed check order, was a C# change in a project that wasn't the identity's, and the eval set had to
keep its own copy of every name the identity used (ADR 0043). F49 wants an eval to be content a person can make,
read and change, and the checks to be catalogue entries a file names ([0056](0056-a-check-is-a-catalogue-entry.md)).

Weighed for the file: an integer version as today (taken), inspect_evals' `N-X` string, and a hash of the folder.
Weighed for a turn's facts: the harness reads them from the engine's events for every eval (taken), a named
extractor, and the identity's own C# hook. Weighed for where a model judge that needs the harness lives: a small
project of its own beside the identity (taken), the check contract growing the judge's base, and the identity
referencing the harness.

## Decision

- **An eval is a folder with an `eval.json`**: its name, the identity it proves, an integer version bumped by hand,
  the sentence it proves, its manual, its modes and its checks by type with their settings. Beside it sit
  `manual.md`, `Dataset/` (as before), any rubric a judge reads, and `Transcripts/`. The file is read strictly
  through records ([0041](0041-json-through-records-by-hand-only-for-bytes.md)): an unknown, missing or null field
  is refused, naming the file and the field.
- **The folder lives beside what it proves**, under `Evals/` in the identity's or pack's project, which copies its
  files to `EvalFiles/{its path from the repository root}/` beside every build. The runner finds every `eval.json`
  there, through the composition, the one project that knows every identity and pack
  ([0027](0027-engine-identities-and-packs.md)); there is no list of evals to keep. `EVALS=` chooses them by the
  identity each names; a name no eval proves is refused.
- **One C# type reads every folder**, `FileEval`; the harness reads a turn's facts from the engine's events for every
  eval. The stub stays C#, to prove the runner with canned answers.
- **Checks come from the catalogue**, in the file's order: the harness's general checks, every identity's and pack's
  own through the composition (`EvalChecks.All`), and a judge that needs the harness from its own project beside the
  identity, which only the runner references, so no host carries the harness. An identity references core and the
  check contract, never the harness; `ArchitectureTests` holds it.
- **A new eval starts as a copy** of the template folder, `evals/template/Example/`: an eval that uses the general
  checks is files only, no C#.

## Consequences

- A new eval, a changed mode or a reordered check is a file change in the identity's folder, reviewed like any
  content; a stored run and its trend read the same, since a check keeps its name and version.
- A check of an identity's own is still C#: a class with an `Entry`, listed by its owner. A judge on the harness's
  judge needs a project beside the identity (`identities/louis-agent.identity.puls.judges`), one more project.
- The runner can't run an eval it can't see: an eval folder outside a project's `Evals/` (the template) is never
  listed, by design.
- The eval's version is bumped by hand, so forgetting it is possible; `dataset.json`'s hash still catches a changed
  scenario.
- A turn's shop hosts were the cook's own reading (from Puls's shop table); with the harness reading the facts, a
  shop tool's site is no longer a host. A passing cook run calls no shop tool, so only a failing run's list is
  shorter.

## Links

[F49 Evals as content](../plans/F49-evals-as-content.md) · [0027](0027-engine-identities-and-packs.md) ·
[0029](0029-simulations-in-csharp.md) · [0041](0041-json-through-records-by-hand-only-for-bytes.md) ·
[0043](0043-repeated-names-are-enums.md) · [0055](0055-keep-it-simple.md) ·
[0056](0056-a-check-is-a-catalogue-entry.md) · [Add an eval](../guides/add-an-eval.md) ·
[louis-agent.identity.puls](../projects/louis-agent.identity.puls.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
