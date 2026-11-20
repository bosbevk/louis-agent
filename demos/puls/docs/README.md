# Puls's docs

> **Reference.** What is written about Puls alone: its setup, how to change it, its design and the decisions only it needs.
> **Elsewhere:** everything about the framework in the [framework's docs](../../../src/docs/README.md).

![Puls's docs: set it up, change it, how it works, why, and a page for site owners](diagrams/README.svg)

Puls is an assistant for people in South Africa, built on the framework as one of its demos. It answers from the web
itself and hands cooking to its cook, a specialist that finds recipes and estimates what the shopping costs. Puls is
paused for now while the framework and its site come first ([the roadmap](../../../ROADMAP.md)), but its code, tests
and docs stay in step. The docs below are the ones about Puls alone, in the order you'd want them: set it up, change
it, see how it works, then the decisions and plans behind it.

- [Setup](SETUP.md), and [what Azure needs](../deploy/README.md)
- Change it: [Puls](guides/change-puls.md), [its web app](guides/change-the-puls-app.md)
- How it works: [identity](projects/louis-agent.identity.puls.md), [judges](projects/louis-agent.identity.puls.judges.md),
  [web app](projects/louis-agent.puls.md), [host-packs](projects/louis-agent.host-packs.md)
- Decisions: [ADR 0081](adr/0081-the-cook-searches-for-itself.md). ADR numbers are shared with the framework's, so a
  number is never used twice.
- [For site owners](SITE_OWNERS.md), [the cook](specs/PULS_COOK.md), [plans](plans/), [features](features/)
