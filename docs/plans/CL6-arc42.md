# Plan: CL6 arc42, ADRs and C4 (cleanup)

> **For:** the TODO's *Architecture docs in arc42* (cleanup) · **Branch:** `docs/CL6-arc42`, stacked on
> `docs/CL5-guides-by-folder` (the earlier three aren't merged yet); its PR goes in after theirs · **Approved:**
> 2026-10-14

## Context

The docs follow Diátaxis since CL4, but the architecture and the reasons behind it have no standard shape:
- **One 405-line `ARCHITECTURE.md`** mixes components, a turn, security, logging and deployment.
- **The decisions are scattered** across the guide's *Rules that always apply*, the specs, the fix docs, the plans and
  the integration-tiers section.

The standard answer, decided with Louis on 2026-10-14:
- **[arc42](https://arc42.org)**: twelve fixed sections for a system's architecture.
- **ADRs**: Architecture Decision Records in Michael Nygard's format. One numbered, never-edited record per decision.
- **The [C4 model](https://c4model.com)** for the diagrams: context, containers, components.

CL4's types stay for the docs that aren't about the architecture: quick start, setup, how-tos and the API reference.

**This is a move, not a rewrite.** Each `ARCHITECTURE.md` section moves to its arc42 section unchanged. Each ADR is
written from text that already records the decision (its context, the decision, its consequences), with links to the
feature, fix or plan where it was made. Nothing the model sees changes. Nothing paid runs, and no new package.

## Design

### `docs/architecture/`: arc42, one file per section (a small file per subject, as CL5 showed for context)

| arc42 section | From |
|---|---|
| `README.md` | The arc42 index, its CC BY-SA attribution, and how to read it |
| `01-introduction-and-goals.md` | *Overview*; the top quality goals (provider portability, cost visible on every request, safety); link to GOAL |
| `02-constraints.md` | .NET 10, Docker, Windows for development with Linux containers, Anthropic by default, a learning project (short) |
| `03-context-and-scope.md` | **C4 level 1**: the users (Rider, web, CLI, Puls), the model providers, Paymo, DevOps, web search, rates, the orchestrator and MCP clients; the interfaces link to AGENT_COMMUNICATION |
| `04-solution-strategy.md` | The key approaches in a paragraph each, each linking to its ADR |
| `05-building-block-view.md` | **C4 level 2** (*Hosts*) and **level 3** for core (*Core abstractions*, *Skills*, *Tools*, *Bootstrap*); links to `projects/` |
| `06-runtime-view.md` | *A chat turn*, *Guards around tool calls*; scenarios that link to AGENT_INTERACTION (a turn) and AGENT_COMMUNICATION (delegation, the orchestrator's fix flow) |
| `07-deployment-view.md` | *Deployment* |
| `08-crosscutting-concepts.md` | *Security*, *Logging*, *Testing*, *Configuration*; the message enums (a link to the core guide) |
| `09-architecture-decisions.md` | The ADR index (a link to `docs/adr/README.md`) |
| `10-quality-requirements.md` | Quality scenarios linking to BENCHMARKS, F18 evaluation and F7 budgets |
| `11-risks-and-technical-debt.md` | A link to the TODO's Known limitations (still the one list), the open fixes, *Rider MCP discovery (partial)* |
| `12-glossary.md` | Toolset, guide, skill, personality, session, sub-session, specialist, peer, ledger, prefix, runbook, Puls, … |

`docs/ARCHITECTURE.md` goes. Every link to it, and to its anchors, is repointed to the new section. The link check
finds them all.

**C4 diagrams in mermaid `flowchart`** (persons, systems and containers labelled by C4 level), not mermaid's
experimental `C4Context` syntax, so they render in both GitHub and Rider.

### `docs/adr/`: Nygard format

Each ADR has *Title, Status, Date, Context, Decision, Consequences* and *Links*. A later ADR supersedes an earlier one;
an accepted ADR is never edited. They are written retroactively, with the date each decision was made (from the
CHANGELOG, specs and plans) and "recorded 2026-10-14".

| ADR | Decision | From |
|---|---|---|
| 0001 | Record architecture decisions | (this plan) |
| 0002 | The Messages API tier with our own loop, through `IChatClient` | Integration tiers section, guide |
| 0003 | Provider strategies: one interface per concern, chosen in `LlmClientFactory` | ARCHITECTURE, guide |
| 0004 | Arguments as data, never through a shell | Guide, *Security* |
| 0005 | Guard the context: summarise oversized results, never run cut-off calls | Guide, CL2 |
| 0006 | Toolsets per session (F5) | F5 plan and doc |
| 0007 | Measure every request: a usage ledger priced from a dated table, unknown as `null` | F1, F2 |
| 0008 | History is append-only | FX1, FX4 |
| 0009 | Message text as enum templates, logging through `ILogger` | CL3 |
| 0010 | Folders by concern, namespace = folder path, held by a test | CL1 |
| 0011 | Prompts are content: plain markdown skills, no provider class per toolset | Guide, *Adding a toolset* |
| 0012 | One orchestrator per microservice | ORCHESTRATOR_DESIGN *Decisions* |
| 0013 | Documentation: Diátaxis types, guides by folder, arc42 and ADRs | CL4, CL5, this plan |

**The root guide's *Rules that always apply*** keeps each one-liner, which Claude needs every session, and links it to
its ADR for the why. Its *Which Anthropic tier* note becomes a link to ADR 0002.

**Process:** a plan that makes a lasting decision adds an ADR. *Finishing a feature* and *Finishing a fix* get that line,
and the plans README says it.

**Not in this plan:**
- A docs site.
- `adr-tools` or other packages: plain markdown, a template in `docs/adr/README.md`.
- Rewriting the specs: they stay explanation, and link to their ADRs.

## Steps (each one commit; a review stop after each)

0. **The plan in the repo,** its row in the plans table, and the TODO line.
1. **ADR scaffold:** `docs/adr/README.md` (index, template, when to write one) and ADR 0001.
2. **ADRs 0002–0007.**
3. **ADRs 0008–0013.**
4. **arc42 sections 1–4,** with the C4 context diagram.
5. **Sections 5–7,** moved from ARCHITECTURE.md, with the C4 container and component diagrams.
6. **Sections 8–12;** ARCHITECTURE.md removed and every link repointed.
7. **The guide, the index and the process:**
   - the rules link to their ADRs;
   - the docs index gets rows for arc42 and the ADRs;
   - "add an ADR" goes into *Finishing a feature*, *Finishing a fix* and the plans conventions;
   - the CHANGELOG entry.

## Verification

- **Link check after every step:** the CL4/CL5 script, extended to scan `docs/architecture/` and `docs/adr/`. Zero
  broken links.
- **No fact lost:** every `ARCHITECTURE.md` section is compared line by line with its new home, as before. Each ADR's
  decision is checked against the source it cites.
- **Diagrams:** Louis opens one section in GitHub and in Rider's markdown preview to confirm the mermaid renders.
- **Nothing the model sees changes:** `Skills/` unchanged since CL5, and the
  `ModelRequestSnapshotTests|SkillFilesTests|ArchitectureTests` tests pass.
- **Size:** the root guide stays at about 6.2k tokens (links only); arc42 and the ADRs load only when read.

## Concepts

- **Architecture as communication (#18):** a standard shape, arc42 and C4, so another engineer can review it.
- **Decisions with their reasons (#18, and every skill):** ADRs keep why each choice was made and what it cost, which
  is the explain-every-decision part of the goal.
