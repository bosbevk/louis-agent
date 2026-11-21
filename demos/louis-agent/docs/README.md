# The coding agent's docs

> **Reference.** What is written about the coding agent alone: its setup, how to change it, its design and the decisions only
> it needs. **Elsewhere:** everything about the framework in the [framework's docs](../../../src/docs/README.md).

![The coding agent's docs: its setup and programs, how to change it, how it works, the demos on the site, and its decisions](diagrams/README.svg)

This folder is the coding agent's own corner of the docs. The coding agent is our demo of the framework at work on
code: a chat and admin you reach from Rider, the command line, MCP or the browser, a triage team, and an orchestrator
that watches a service and fixes its errors. Start with the setup, then read how it works; the list below is the
place to look things up.


- [Setup](SETUP.md), [Quick start](QUICK_START_AGENT.md)
- Its programs, each naming what the framework's host loads: `hosts/LouisAgent.CodingCli`, `hosts/LouisAgent.CodingAcp`, `hosts/LouisAgent.CodingMcp`, `hosts/LouisAgent.CodingEvalRunner`, `hosts/LouisAgent.CodingApi`
- Change it: [the web app and its site](guides/change-the-web-app.md),
  [the orchestrator](guides/change-the-orchestrator.md)
- [The demos](demos.md): what each demo on the site shows, how it was made, and what it would mean for a business
- How it works: [the coding agent's chat and admin](projects/LouisAgent.CodeReview.md), [the framework's site](../../../src/docs/projects/LouisAgent.Site.md) and [the guide in its corner](../../../src/docs/projects/LouisAgent.Identities.Guide.md),
  [the triage team](projects/LouisAgent.Identities.Triage.md), [orchestrator](projects/LouisAgent.Orchestrator.md), [its runbooks](projects/orchestrator-runbooks.md)
- Decisions: [ADR 0012](adr/0012-one-orchestrator-per-microservice.md), [ADR 0066](adr/0066-the-site-is-static-razor.md) (superseded in part),
  [ADR 0077](adr/0077-films-on-github-are-rendered-webp.md). ADR
  numbers are shared with the framework's, so a number is never used twice.
- [The orchestrator's design](specs/ORCHESTRATOR_DESIGN.md), [features](features/)
