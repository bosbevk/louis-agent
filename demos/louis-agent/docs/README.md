# The coding agent's docs

> **Reference.** What is written about the coding agent alone: its setup, how to change it, its design and the decisions only
> it needs. **Elsewhere:** everything about the framework in the [framework's docs](../../../src/docs/README.md).

- [Setup](SETUP.md), [Quick start](QUICK_START_AGENT.md)
- Its programs, each naming what the framework's host loads: `hosts/louis-agent.coding-cli`, `hosts/louis-agent.coding-acp`, `hosts/louis-agent.coding-mcp`, `hosts/louis-agent.coding-eval-runner`
- Change it: [the web app](guides/change-the-web-app.md), [the site](guides/change-the-site.md),
  [the orchestrator](guides/change-the-orchestrator.md)
- How it works: [web app](projects/louis-agent.web.md), [site](projects/louis-agent.site.md),
  [orchestrator](projects/louis-agent.orchestrator.md), [its runbooks](projects/orchestrator-runbooks.md)
- Decisions: [ADR 0012](adr/0012-one-orchestrator-per-microservice.md), [ADR 0066](adr/0066-the-site-is-static-razor.md). ADR
  numbers are shared with the framework's, so a number is never used twice.
- [The orchestrator's design](specs/ORCHESTRATOR_DESIGN.md), [features](features/)
