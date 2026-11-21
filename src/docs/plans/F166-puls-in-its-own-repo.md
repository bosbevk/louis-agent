# F166 Puls in its own repository

A plan, a parent with four children: Puls leaves the framework's repository for its own, `github.com/bosbevk/puls`,
and builds on the framework as published packages, so the framework names no product of its own and a product on it
is just a user of its packages.

## Context

**What Louis asked** (2026-11-19). "can we move everything puls to the pulse repo?", then "we need to move it". His
picks of that day, each of three options after research:

| # | The choice | His pick |
|---|---|---|
| 1 | How Puls gets the framework from another repository | C: the framework is published as NuGet packages first, and Puls references them, over a git submodule (A) or the two repositories side by side (B) |
| 0 | The framework's licence, before it is published | A: the PolyForm Noncommercial License 1.0.0, done in #425 (ADR 0127) |
| 2 | Puls's packs | C: `calculator` stays a framework pack; `recipes`, `shop-prices` and `where-to-buy` move with Puls |

**What was looked up.** `demos/puls` holds 787 tracked files and 20 projects in the solution; nothing outside it
references it, and nothing in the framework references the three packs that move. But it also holds what is the
framework's: the API's own tests (`LouisAgent.Api.Tests`, `.api.evals.tests`, `.api.integration-tests`,
`.api.standin`, `.api.testing`, which test `src/hosts/LouisAgent.Api`), the Azure deploy that releases the two sites
(`deploy/azure/deploy.ps1`, its README and architecture, the Bicep build under `config/azure`), the deploy's tests in
`LouisAgent.Puls.Tests`, and the workflow `deploy-puls.yml`, which releases only the two sites. `host-packs` is the
one project that knows every pack. The `puls` repository exists, empty but for an Apache 2.0 licence. The framework is
not published as packages yet (the TODO's known limitations).

**Not settled yet, each a finding when its child starts:** where the packages are published, how they are
versioned and which projects become packages (F166.2); whether Puls keeps its history, and its licence (F166.3).

## Design

### The shape, in plain words

**The framework takes back what is its own first.** The API's tests go to `src/tests/hosts/`, the deploy and its
docs to the sites' side, the deploy's tests beside them, and the workflow is named for what it releases. Nothing
changes in behaviour, so `main` stays green at every step.

**Then the framework publishes itself.** The engine, the hosts, the web and the general packs become packages under
ADR 0127's licence, built and versioned by a workflow.

**Then Puls leaves**, with `recipes`, `shop-prices` and `where-to-buy` and their tests, checks and web view, into the
`puls` repository, referencing the framework only as packages. Its coming-soon page and its workflow go with it.

**Last, louis-agent forgets it**: the solution, CI, the repo map, the docs and the ADRs that name Puls as here.

### Architecture

![The framework publishes packages from louis-agent; Puls, in its own repository with its three packs, references those packages](diagrams/F166-puls-in-its-own-repo-architecture.svg)

### The children, in order

![The framework takes back its own parts, publishes itself, then Puls leaves and louis-agent forgets it](diagrams/F166-puls-in-its-own-repo-communication-the-order.svg)

| Child | What | Done when |
|---|---|---|
| F166.1 | The framework's parts out of `demos/puls`: the API's tests, the deploy and its docs and tests, the Bicep build, the workflow renamed | Nothing the framework needs is under `demos/puls`; every test passes where it now stands; the sites still release |
| F166.2 | The framework as NuGet packages: which projects, where, how versioned, the publish workflow | A clean project outside this repository builds an agent from the published packages alone |
| F166.3 | Puls and its three packs in the `puls` repository, on the packages, with its own CI and coming-soon page | `puls` builds and its tests pass in its own repository, referencing no path into louis-agent |
| F166.4 | Puls gone from louis-agent: the solution, CI, the repo map, the docs, the ADRs that name it as here | No tracked file under `demos/puls`, the three packs gone, the full suite green |

Each child has its own plan, branch, pull request to `main` and write-up; a child's plan is written when it starts.

## Verification

Each child's own. Across the four: `main` is green after every merge, the two sites keep releasing, and at the end
nothing in louis-agent references Puls.

## Concepts, and where they sit in GOAL.md

- **The engine split** (F166.1, F166.4): a framework that names no product, a dependency one way only. *Skill 20,
  extensibility.*
- **Publishing a framework** (F166.2): packages, versions, a licence on each. *Skill 18, testing and shipping.*
- **A product on a framework** (F166.3): Puls as the framework's first outside user. *Skill 20, extensibility.*

## Approved during the build

---
[Docs index](../README.md) · [Plans](README.md) · Related: [ADR 0127](../adr/0127-licence-polyform-noncommercial.md) · [ADR 0027](../adr/0027-engine-identities-and-packs.md) · [ADR 0076](../adr/0076-core-ships-no-agent.md)
