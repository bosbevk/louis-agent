# 0092. A project holds what is reused together, and its name says where it sits

**Status:** Accepted · **Date:** 2026-11-07 (Louis, on `louis-agent.choices` and `louis-agent.session-stream`: "this
can move into composition?" and "and session-stream?"; of three options he picked **A**, keep them apart; then, on
the six places that don't follow it: "we will fix all of it in one big CL now", "this must be an adr") · **Recorded:**
2026-11-07

## Context

The engine is becoming a set of packages (ADR 0076, ADR 0091). A package is taken whole: whoever references it gets
every type and every package it references, and in a browser app every one of those is downloaded. Two small
projects sit below core and reference nothing: `choices` (the one enum-name parser) and `session-stream` (the
reader of a session's events). The browser app takes both and not `composition`; the eval protocol and the API
contracts take `choices` alone. Folding them into `composition` would hand the browser the engine's contracts and
two abstraction packages it never uses.

A map of every project's references, made the same day, found six places where a project forces its users to take
what they don't use: the eval studio inside the API (over half of it, with MongoDB) and inside the framework's web
(about nine thousand lines, in every site's download); three model SDKs inside core; Rider's tool discovery inside
core, started by every host; the API referencing `core.web` for two lines; the eval runner library starting its own
telemetry.

## Decision

**A project holds what its users reuse together, and nobody is made to take what they don't use.** This is Robert
Martin's *Common Reuse Principle*: classes reused together are packaged together; a package's users must take all
of it, so a package holds nothing a user of it would not want. Its three rules:

1. **A plain name sits below core and references nothing heavy.** `choices`, `session-stream`, `composition`, the
   contracts projects (`api.contracts`, `accounts.contracts`, `evals.protocol`) take at most each other and
   Microsoft's abstraction packages, so the browser, the engine and a host can all take them. They stay apart from
   each other when their users differ.
2. **A dotted name sits on the thing before the dot, and the thing before the dot opts in to nothing.** `core.web`,
   `core.telemetry`, `core.anthropic`, `core.ollama`, `core.openai` sit on core; `api.evals` on the API;
   `web.accounts`, `web.evals` on the framework's web; `pack.X.web` on a pack. The owner keeps a seam and a Null
   Object (ADR 0091) or a slot, and a program or a site composes: `HostServices` for a host, `AddLouisAgentX()` and
   the router's assemblies for a site.
3. **A product, an SDK, a binary or a store never sits in core or in a plain-named project.** Chrome, the tracer, a
   model's SDK, MongoDB, Rider: each is a library on the thing it serves. Core knows a provider's *name* (a setting's
   value, `LLM_PROVIDER=anthropic`) but not its SDK.

CL57 applies the rule to the six places: the API reads what the web is from the seam; the runner's programs hand in
the telemetry; Rider's discovery becomes the ACP server's behind a discovery seam; the SDKs become three libraries
on core behind a provider seam; the eval studio becomes `louis-agent.api.evals` and `louis-agent.web.evals`, a site
opts in.

Weighed and not taken: fold `choices` into `composition` (one project fewer; the browser and the contracts projects
would take the engine's contracts); fold both (the contracts project becomes "everything shared"); keep the six as
they were (every site ships an eval studio and three SDKs).

## Consequences

- A new thing is placed by asking who reuses it with what: with the engine's contracts, in `composition`; on core
  with an SDK, a `core.X`; on a host or the web as a thing a site opts into, `api.X` or `web.X`.
- `accounts` keeps its reference to `composition` for the message machinery, the one outsider that does; a second
  one makes the case for a `louis-agent.messages` below `composition`.
- The API's contracts stay one project; the eval studio's records are small, and the first taker of the API
  contracts without the studio makes the case for splitting them.
- The rules tests (`CorePackagesTests`, `ArchitectureTests`, the solution layout tests) hold what this ADR says
  for the projects they cover.

## Links

[CL57's design docs](../projects/README.md) · [ADR 0091](0091-web-access-and-telemetry-are-libraries-on-core.md) ·
[ADR 0089](0089-the-account-client-is-a-library-a-site-opts-into.md) · [ADR 0076](0076-core-ships-no-agent.md) ·
[ADR 0003](0003-provider-strategies.md) · [ADR 0045](0045-typed-answers-and-a-shared-contract.md) ·
Robert C. Martin, *Agile Software Development* (the package principles)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
