# Draft: Phase 10: the engine as a developer package

> **Plan draft, not yet approved.** Moved whole out of the old platform plan on 2026-11-07 (CL56), lines 3801 to 3883 of `platform.md` at the tag `pre-roadmap`; the body below is as it was. When the work starts, this is rewritten into the plan Louis approves (the development guide, rule 3) and its TODO row names it. The sub-feature split of 2026-10-28 is the TODO's rows.
>
> **Changed 2026-11-07** (Louis: "focus on the framework first then build the agents"): *When* below no longer
> holds for Stage A. Packaging in place (F74) is part of [the framework's track](F106-framework-track.md) and can start
> before the engine features are done; Stage B, the engine's own repo, still waits for them.

### Phase 10 · The engine as a developer package, after every engine feature (Louis, 2026-10-20)

Louis, 2026-10-20: "so I want to make the engine its own repo with package"; "all outstanding agent features need to
be implemented first. This will power Puls and all future AI workflows I'm going to take on. And it is something devs
can use, not business." The details, diagrams and ADR 0035/0036 drafts are in `plans/engine-package.md` (page
<https://claude.ai/artifact/AQda2Cz1wvLyktbbKE8MeE>); the decisions:

- **What it is:** a developer's product. Businesses meet it only through what's built on it (Puls, a hosted agent).
  Accounts, tenants, quotas, a tenant's POPIA report, billing, hosting and the approval buttons stay on the hosting side,
  never in the package.
- **When:** after every engine feature: Phases 3 to 9, then *Later*'s engine items (F45, F9, F10, a tool added
  mid-chat, F38, planning and a self-check, computer use, images as input), with CL17 widened (core keeps no tools)
  before the move. Puls's track, F40, voorman's packs and hosting are not engine work.
- **Stage A, packaged in place** (six steps, one pull request each): core grants no internals to hosts; its public
  surface declared (PublicApiAnalyzers); a reproducible build (deterministic, Source Link, embedded PDBs, lock files,
  central package management, NuGet audit); packed with MinVer from `core-v*` tags, with `prompts.sha256` (the ledger's
  prompt hash per embedded prompt) and package validation; a consumer test built from the package alone that also
  replays a recorded simulation; a publish behind a `release` environment Louis approves, least-privilege tokens,
  SHA-pinned actions and build provenance.
- **Stage B, its own repo:** `louis-agent-engine`, made with `git filter-repo` so history moves with it; this repo then
  takes the engine as packages.
- **What the engine repo ships:** `LouisAgent.Core` (no tools, no product, no facts); the general packs
  `LouisAgent.Packs.Files`, `.Web`, `.Shell`, `.Git`, `.DotNet`, `.Delegation`, `.Skills`; `LouisAgent.Packs.Shared.*`
  (libraries packs share, never a pack: nothing in it implements `IAgentPack` or defines a tool, and it references no
  pack); `LouisAgent.Simulation`, `LouisAgent.Testing` and the providers; and its reference agent:
  `LouisAgent.Identities.Coding` (CL17's identity) with `LouisAgent.Hosts.Acp` for Rider, and `LouisAgent.Hosts.Api`, the
  agent API once it names no product (Louis, 2026-10-24, *One agent API*). The coding agent "knows
  everything about himself, to help devs": the engine's docs ship in the same version and are read on demand through
  `LouisAgent.Packs.EngineGuide`. It knows the engine's features, not the developer's code (Louis: "he will not really
  know your code, he will tell you how to implement features he has"), and is measured by a "how do I" simulation:
  developer questions whose answers are graded for the right contracts and steps from the guide. It also sees its own
  code (Louis: "he will know because he will see his own code"): the engine's source for the installed version ships in
  the guide pack, read on demand like the docs; who receives that source is settled with the licence and the feed. **The aim beyond that** (Louis, 2026-10-20:
  "he will eventually just be able to build with himself, don't know how to do it yet"): the coding agent builds with
  the engine itself (writes a developer's packs and identities, and in time extends the engine), so *how* is an open
  question, not a design yet. The guide and the "how do I" simulation are its first measurable step; it sits
  beside build mode's side note in *Later* and the agent-built tools that already wait for approval (FX20, ADR 0033). This repo keeps Puls, the demo, voorman and
  the product packs (shops, recipes, places, customers, triage, fixing, Paymo, DevOps).
- **Two versions** (Louis, 2026-10-20: "we have 2 versions, one with the AI you must pay"; "it comes with no tools...
  you will have to extend it, if you want certain nice to haves then you must pay"): **free** is core with no tools,
  the providers, the test fakes, the simulation harness (proposed free, to confirm) and one sample pack,
  `LouisAgent.Packs.WebSearch` (Louis: "we can give it websearch"): search only, on the developer's own provider,
  results framed as untrusted, while fetching pages and the browser stay in the paid `Packs.Web`;
  **paid** adds the general packs and `Packs.Shared`, the coding identity, the guide pack with the engine's docs and
  source, and the Rider host. Both run on the developer's own model API key: we sell code, never tokens. Decided now
  so no licence changes later; the licences and feeds are chosen at F44.
- **Facts are data:** prices and model facts are loaded from the file or URL the host names, in our JSON layout
  (F15, above).
- **Versions:** SemVer, `0.x` until the contracts settle; a prompt change, or a default model, thinking level or round
  limit changed, is at least a minor version; an upgrade in this repo is a pull request gated by the simulations.
- **100% C#:** the Python checks go in Phase 3, and from then `ArchitectureTests` fails on a `.py` file outside
  `Skills/`.
- **Parked by Louis:** the feed (GitHub Packages, private, or nuget.org) and open core. If open core is ever chosen,
  the line is the engine for developers (free) and our hosting (ours), and F7's plan (Phase 5) and F43's must put their
  contracts in core (`IBudgetPolicy`, `IRequestEstimator`, `IBudgetApproval`, `ITenantContext`,
  `IComplianceRecorder`), so decide before F7's plan.

![The engine and its general packs in their own repo as packages, and the products in this one building on them](diagrams/phase-10-engine-package-phase-10-the-engine-as-a-developer-package-after-every-engine-feature-louis-2026-10-20.svg)
