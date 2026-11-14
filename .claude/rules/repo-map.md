# Where things are in the repo

Loaded in every session, with `src/docs/CLAUDE.md`.

Where things are. What's inside each project is in its design doc in [projects/](../../src/docs/projects/README.md); core's folders
in [Project layout](../../src/engine/louis-agent.core/CLAUDE.md#project-layout).

```
src/engine/              # louis-agent.core (the loop, delegation, web access and its guards, skills, providers, usage, bootstrap), .choices (the one enum-name parser), .session-stream, .composition (the contracts everything agrees on); louis-agent.core.web (the browser, search, robots, rendering, the recorders), .core.telemetry (the tracer, the collector's sink), .core.anthropic, .core.ollama and .core.openai (one model SDK each), each on core behind a seam, a host opts in (CL33, CL57)
src/hosts/               # louis-agent.cli, .acp (Rider, with its tool discovery), .api (sessions + SSE; serves a web app), .api.evals (the eval studio, a program opts in), .api.contracts, .accounts, .accounts.contracts, .mcp (the tools, no model)
src/web/                 # louis-agent.web: the framework's web, what every site shares; louis-agent.web.accounts: the account pages, louis-agent.web.evals: the eval studio's pages, a site opts into each; louis-agent.diagrams: the diagrams drawn from data, plain C# the web and the docs' tool share
src/evals/               # The eval harness, its protocol, the checks contract, the runner and the template folder
src/site/                # The framework's own website (F124, ADR 0111): louis-agent.site (the visitor area), louis-agent.identity.guide (the guide in its corner), louis-agent.identity.showroom (three made-up businesses' agents), louis-agent.site-api (its host), config/, docker/, deploy/; the one place in src/ that uses packs and an identity, and nothing else in src/ references it
packs/                   # What an agent can do, one area (CL32): tools/ (the packs, tools only: files, git, dotnet, shell, web, paymo, devops, calculator, where-to-buy, shop-prices, recipes, docs, leads, site, triage), evals/ (what proves a pack, apart so someone can take the tools without it; only the runner references these), tests/ and web/ (a pack's own tests and its own web views, apart from its tools, ADR 0086), docs/
demos/puls/              # A demo, as someone's own project would be: identity/ (who it is), evals/ (an eval is a folder of files), web/ (its app), hosts/ (host-packs picks its packs), config/, docker/, skills/ (what the agent writes), deploy/ (Azure), tests/, docs/, and git-ignored data/ and logs/
demos/louis-agent/       # Our own demo: identity/ (the coding agent, the triage team), web/ (code review: the coding agent's chat and admin), hosts/ (the orchestrator POC, and the programs that start the framework's hosts), config/, docker/, skills/, tests/, docs/, and git-ignored data/ and logs/
demos/starter/           # The smallest site (web/), its API program (hosts/) and what `dotnet new louis-site` makes ([Spawn a site](../../src/docs/guides/spawn-a-site.md))
src/tools/               # louis-agent.usage-probe: a provider's raw usage; prices and totals a ledger. louis-agent.film-export: the site's films to WebP. louis-agent.doc-diagrams: the docs' diagrams from their JSON
src/tests/               # Only the framework's tests, grouped like src/ ([tests](../../src/docs/projects/tests.md)); a demo holds its own tests/ and packs/tests/ the packs'; the ones that leave their process are *.integration-tests
src/docs/                # The framework's docs: this guide, adr/, guides/, projects/, plans/, fixes/, learning/, architecture/
.claude/                 # What Claude Code reads: rules/ (the how of a kind of file, loaded by path; the design rules and this map, always), skills/ (a procedure per job), README.md (how they are laid out)
```
