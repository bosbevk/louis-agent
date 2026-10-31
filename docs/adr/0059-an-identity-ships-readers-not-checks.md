# 0059. An identity ships readers, not checks

**Status:** Accepted · **Date:** 2026-10-30 (Louis's pick on the F49.2b plan's finding G) and 2026-10-31 (his picks
during the build) · **Recorded:** 2026-10-31

## Context

After F49.2 the harness had six general checks and Puls had six of its own: no failed turn, within the budget, an
image from the recipe, serves as asked, and two shop rules. Four of Puls's were general rules wearing Puls's
knowledge: "read a value from the answer, compare it with the scenario's". Only the reading was Puls's (the rand total
on an Afrikaans *Totaal* line, the people served in Afrikaans number words, a recipe page's share buttons that are
no photo). Every eval tool ships the same families of check (run facts, text rules, structure, tool calls, compares,
similarity, language, a model judge); the harness had a few of each family at most.

Weighed for the shape of a compare: the reader fixed in C# by an identity's entry (a named specialisation, dbt's
generic test with fixed arguments), a subclass with defaults (Great Expectations), and a reader named in the file
beside the key (taken).

## Decision

- **The harness ships every family a standard eval tool ships**, as general checks in `GeneralChecks.All`, a folder
  per family under `Grading/Common/`. A family that needs a model or a provider we don't have waits until an eval
  asks for it (`similar`, embeddings: the TODO).
- **Extract, then compare.** A check that compares a value in the answer with the scenario's is a general compare
  plus a *reader*, a one-method Strategy of one kind (`IMoneyReader`, `INumberReader`, `IImageReader`, in the check
  contract) that the eval file names beside the key: `{"type": "amount-under", "name": "within-budget",
  "parameters": {"key": "budget", "reader": "rand-total"}}`. Readers are a second registry, `ReaderCatalog`, built
  like the check catalogue: the harness's plain ones and every identity's and pack's through the composition
  (`EvalReaders.All`). A name used twice, an unknown one or a reader of the wrong kind is refused when the file loads.
- **An identity ships readers, not checks**, and a check of its own only for a rule no general check and reader can
  say. Its file keeps the names its stored grades carry with `name`.
- **A swap of one check for another is proven, not assumed**: every grade's outcome and score is pinned on the eval's
  transcripts before the swap and must hold after (`CookGradesTests`), and the baseline is replayed and compared. A
  changed check version bumps the eval's version.

## Consequences

- Puls's `CookNoError`, `WithinBudget`, `RecipeImage` and `ServesAsAsked` are gone; its readers (`rand-total`,
  `servings`, `recipe-photos`) are Adapters over its scoring code, unchanged, so its knowledge didn't move and no grade
  did (230 pinned on 26 transcripts, 140 of 140 on the replay).
- A general reason is less specific than a domain's: `within-budget` no longer says whether the total came from the
  total line or the price rows. The outcome is the same.
- The cook's eval went to version 11, so its trend starts a new line where `within-budget` and `recipe-image` became
  version 1 of the general checks.
- `cook-no-error` is the general `no-error`, which fails a trial that both broke and errored instead of calling it
  Unknown.
- A third kind of value (a date, a list) is a new reader interface and a new compare, not a change to the old ones.

## Links

[F49 Evals as content](../plans/F49-evals-as-content.md) · [0027](0027-engine-identities-and-packs.md) ·
[0055](0055-keep-it-simple.md) · [0056](0056-a-check-is-a-catalogue-entry.md) ·
[0057](0057-regex-is-the-last-option.md) · [0058](0058-an-eval-is-a-folder-of-files.md) ·
[Add an eval](../guides/add-an-eval.md#4-choose-the-checks) · [write-up 12](../learning/12-evaluation.md) ·
[write-up 20](../learning/20-extensibility.md)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
