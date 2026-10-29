# Architecture

> **Explanation.** How louis-agent is built and why, in [arc42](https://arc42.org)'s twelve standard sections, with
> diagrams at the [C4 model](https://c4model.com)'s levels. **Elsewhere:** the decisions themselves, one record each, in
> [architecture decisions](../adr/README.md); a project's files and types in [projects/](../projects/README.md).

Read sections 1, 3 and 5 for the system at a glance; the others when you need them. Each section is a short file of
its own, so a reader (or Claude) loads only the one a question needs.

| # | Section | Answers |
|---|---|---|
| 1 | [Introduction and goals](01-introduction-and-goals.md) | What it is, what matters most, who it is for |
| 2 | [Constraints](02-constraints.md) | What was fixed before any design: platform, providers, rules |
| 3 | [Context and scope](03-context-and-scope.md) | Who and what it talks to (C4 level 1) |
| 4 | [Solution strategy](04-solution-strategy.md) | The few approaches everything else follows |
| 5 | [Building block view](05-building-block-view.md) | The parts: containers (C4 level 2) and core's components (level 3) |
| 6 | [Runtime view](06-runtime-view.md) | A chat turn, the guards, and the scenarios across agents |
| 7 | [Deployment view](07-deployment-view.md) | Where each part runs: images, services, mounts |
| 8 | [Crosscutting concepts](08-crosscutting-concepts.md) | Security, messages, logging, testing, configuration |
| 9 | [Architecture decisions](09-architecture-decisions.md) | The ADRs that shape the most |
| 10 | [Quality requirements](10-quality-requirements.md) | How the quality goals are checked |
| 11 | [Risks and technical debt](11-risks-and-technical-debt.md) | Known limitations, open fixes and decisions |
| 12 | [Glossary](12-glossary.md) | The words these docs use in a sense of their own |
| — | [The evals](evals.md) | How the evals are built and how a run talks: the harness, the runner, the protocol, the portal |

The template is arc42 by Dr. Gernot Starke and Dr. Peter Hruschka, [arc42.org](https://arc42.org), used under CC BY-SA
4.0.

---
[Docs index](../README.md) · Related: [Architecture decisions](../adr/README.md) · [Projects](../projects/README.md)
