# 0073. Packs and identities reference no evals

**Status:** Accepted · **Date:** 2026-11-04 (Louis, shown a new reference from the shop-prices pack to the check
contract: "this is not good for our spilt we intend", then *Change ADR 0060 now*, "always take the engien split into
account", "if we need to schare contract it needs to be in a shared project or duplicated", "fix this now here", *a
checks project per pack*, for Puls "do it for identities also now", and then "checks is there to do evals and should be
seperate, someone could for instance use the tool but do not want the evals. Or do not own evals") · **Recorded:**
2026-11-04

Supersedes [ADR 0060](0060-a-pack-ships-its-checks.md).

## Context

ADR 0060 said a pack ships the checks that prove its tools, so a pack may reference the check contract
(`evals/louis-agent.evals.checks`), and Puls's identity did the same for its evals. When a measure of unpriced items was
built that way, the shop-prices pack, until then on core alone, gained that reference, and Louis stopped it.

The split is the engine as its own repo and package (Phase 10 in the [TODO](../../../TODO.md): F44.2 split packages, F44.3
packaged in place, F44.4 its own repo). Every host loads a pack and an identity, so whatever they reference comes along
wherever they go: leaning on the evals ties what an agent can do, and who it is, to how we prove it.

Someone can also take the tools or an identity and want no evals at all, or not own them, so what proves them can't
live in the same folder.

Weighed for where the existing checks go: a checks project per pack (taken), in `packs-evals/`; one project for all of
them, which would know every named product and grow with each pack; and
rewriting them as general checks with the particulars in the eval file, which is a rewrite of about 550 lines that
could change pinned grades. The pattern is Separated Interface (Fowler, *Patterns of Enterprise Application
Architecture*), as `louis-agent.identity.puls.judges` already did.

## Decision

- **No pack and no identity references anything under `src/evals/`.** A pack and an identity stand on core alone.
- **What proves one is a project of its own, which only the runner references:**
  `packs-evals/louis-agent.pack.{name}.checks`, `demos/{name}/evals/louis-agent.identity.{name}.evals` and `.judges`.
  `packs/` holds tools and nothing else ([ADR 0074](0074-the-folders-say-what-ships.md)). The arrow points at the thing proved,
  never the reverse; a project references what it uses and no more. The composition names none of them, so no host
  carries one.
- **The lists meet in the runner.** `RunnerChecks` and `RunnerReaders` name each project's list; the composition's
  `EvalChecks` and `EvalReaders` are gone.
- **A new check is a general one in the harness when it can be said generally** (`tool-rows-marked`), with the
  particulars in the eval file; a rule that can't goes in the checks project. The harness names no product.
- **A contract two sides share goes in a shared project, or is duplicated**, never reached through a reference across
  the split. A duplicate has a test in the composition's tests holding the copies equal (`UnpricedMarkTests`,
  `ShopTableTests`).
- **Every placement is weighed against the split:** before a new reference or a new type's home, say which side of the
  split each project lands on and whether this crosses it.

ADR 0060's other rules stand: what only an identity knows comes in through a reader the file names, and plain markdown
reading is the check contract's.

## Consequences

- `ArchitectureTests` holds it: `NoPack_ReferencesTheEvals`, `APacksChecksProject_ReferencesOnlyItsPack_AndTheContract`,
  `TheIdentity_ReferencesOnlyTheEngine`, `TheComposition_ReferencesNoChecksOrEvalsProject` and
  `ToolsAndIdentitiesFolders_HoldNoEvalsProject`.
- The move kept every namespace (each new project's root namespace is the old one) and changed no rule, so the cook's
  pinned grades didn't change.
- An eval's folder is `demos/{name}/evals/louis-agent.identity.{name}.evals/Evals/{Folder}/`, and the studio publishes there
  (`PublishTarget`), so a new identity needs its evals project before an eval can be published for it.
- The API references the check contract directly, for the kinds of reader its studio lists; before, it had it through
  the identity without saying so. The contract holds no check.
- Puls's name enums for its evals live with them (`Evals/Checks/Shared/`), so a Puls test that needs one references the
  evals project.

## Links

[0027](0027-engine-identities-and-packs.md) · [0028](0028-packs-stay-convertible-to-mcp.md) ·
[0056](0056-a-check-is-a-catalogue-entry.md) · [0059](0059-an-identity-ships-readers-not-checks.md) ·
[0060](0060-a-pack-ships-its-checks.md) · [Build a pack](../guides/build-a-pack.md#9-its-checks-when-it-proves-its-tools)

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
