---
name: site-pages
description: The framework's site's pages in reading order, with their sections, so the framework Orbi knows which page and section to open without searching for it.
metadata:
  level: agent
  load: always
---
# The pages of this site

These are every page there is, in the order a developer reads them. Each section has its own address under its
page, listed in the page block when they are on it. Open one with `ShowPage` when it answers what someone asks.

## Never

1. **Never open or name a page that isn't on this list.** A page that isn't there loses them.
2. **Never say how the evals work today.** They are being rebuilt from scratch. Their sections say only what the
   plan builds, said as planned and never as built, and that is all there is to say.

## The pages

- `/`, the Overview: what louis-agent is at a glance, a few lines of your own program, the whole framework in its
  five families (the engine, the hosts, the web, the evals, the tools) with every project it ships, the sites, demos
  and packs built on them, why C#, and that it runs on your own hardware.
- `/getting-started`: your agent in two lines, the coding agent's real program; what it loads, the identities and
  packs; whose files are whose; its settings, then run it; and where next, the guides that make it yours.
- `/how-it-works`: one turn through every layer; the web with the backend; who an agent is, the identities; what an
  agent can do, the packs; and the rules every turn keeps.
- `/features`: everything it does, by area: the engine, any model, where it runs, the web, its guards, what it
  measures, and the code.
- `/build-your-own`: make an agent of your own, in a few steps each, with the guide for each: an identity (who it
  is), a pack (what it can do), and a site (your agent on a website, from the starter template).
- `/run-it`: run it in Docker on your own machine, the two stacks and the commands that start them; watch every turn
  on the dashboard; and how the two sites reach Azure on every merge.
- `/cost-and-evals`: what a turn costs to the cent, the ledger, the price table and one request worked to rand, then
  the evals' places, which say what their redesign plans: one file of conversations a judge marks, and one page
  per run.
- `/examples`: the agents it already runs, Puls, the coding agent and the guide, and what's coming.
- `/comparison`: louis-agent beside Microsoft Agent Framework and Semantic Kernel, and where each cell comes from.

## An example

A developer asks what a pack is. How it works has a section for it, so you open `/how-it-works` at its packs with
`ShowPage` and say in one line what to look at there.

## References

- [`src/docs/projects/louis-agent.web.framework.md`](https://github.com/bosbevk/louis-agent/blob/main/src/docs/projects/louis-agent.web.framework.md), the pages and what each tells
