# Architecture decisions

> **Reference.** Every lasting architecture decision, one record each, newest last. **Elsewhere:** how the system is
> built in the [architecture](../architecture/README.md), how a change is made in the [development guide](../CLAUDE.md).

Each record is an ADR (architecture decision record) in
[Michael Nygard's format](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions): the context that
forced a choice, the decision, and what it costs. Decisions made before 2026-10-14 were recorded
afterwards from the docs that hold them (each ADR links its sources), with the date they were made.

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Accepted |
| [0002](0002-messages-api-tier-with-our-own-loop.md) | The Messages API tier, with our own loop behind `IChatClient` | Accepted |
| [0003](0003-provider-strategies.md) | Provider differences are strategies, chosen once in `LlmClientFactory` | Accepted |
| [0004](0004-arguments-as-data.md) | Arguments travel as data, never through a shell | Accepted |
| [0005](0005-guard-the-context.md) | Guard the context: summarise oversized results, never run cut-off calls | Accepted |
| [0006](0006-toolsets-per-session.md) | A session chooses its toolsets when it opens, and keeps them | Accepted |
| [0007](0007-measure-every-request.md) | Measure every request, priced from a dated table | Accepted |
| [0008](0008-history-is-append-only.md) | Conversation history is append-only | Accepted (FX4 still open) |
| [0009](0009-message-text-as-enum-templates.md) | Message text lives in enum templates; logging goes through `ILogger` | Accepted |
| [0010](0010-folders-by-concern.md) | Folders by concern, the namespace is the folder path, and a test holds it | Accepted |
| [0011](0011-prompts-are-content.md) | Prompts are content: plain markdown skills, no provider class per toolset | Accepted |
| [0012](0012-one-orchestrator-per-microservice.md) | One orchestrator per microservice, which asks louis-agent for fixes | Accepted (its tier question closed by 0031) |
| [0013](0013-documentation-structure.md) | Documentation: one type per doc, guides by folder, arc42 and ADRs | Accepted |
| [0014](0014-test-first.md) | Test first: a step's tests are written and seen failing before the code | Accepted |
| [0015](0015-licence-agpl.md) | Licence: AGPL-3.0, so improvements to a hosted Puls stay open | Superseded by 0016 |
| [0016](0016-licence-all-rights-reserved.md) | Licence: all rights reserved | Accepted |
| [0017](0017-skills-by-level.md) | Skill files follow the levels: a folder per level, and a role opens only at its own | Accepted |
| [0018](0018-built-in-prompts-embedded.md) | Built-in prompts are embedded, read only from the assembly | Accepted |
| [0019](0019-modes-one-file-per-product.md) | A level-0 session is a mode, in a folder per product; a language shares its product's routing | Accepted |
| [0020](0020-principles-for-every-agent.md) | Every agent loads the same principles first: never lie, cheat, steal or murder | Accepted |
| [0021](0021-minions-one-task-one-tool.md) | Minions: a specialist hands one task and one of its own tools down, two levels deep and no further | Accepted |
| [0022](0022-benchmarks-recorded-from-the-protocol.md) | Benchmark results in MongoDB, recorded by the API from the harness's stdout protocol | Amended by 0029 |
| [0023](0023-copyleft-service-beside-us.md) | A copyleft service may run beside us, unchanged; what we link stays MIT, Apache or BSD | Accepted |
| [0024](0024-choices-are-enums.md) | Choices are enums, not strings, parsed once where they enter | Accepted |
| [0025](0025-a-role-declares-its-toolsets.md) | A role declares its own toolsets in its file; a `?` toolset loads only where an instance has it | Accepted |
| [0026](0026-parallel-tool-calls-for-read-only-toolsets.md) | A round's tool calls run at once only where every toolset is parallel-safe | Accepted |
| [0027](0027-engine-identities-and-packs.md) | The engine, identities and packs: core names no product | Accepted |
| [0028](0028-packs-stay-convertible-to-mcp.md) | Packs stay convertible to MCP; MCP plugs in at the toolset | Accepted |
| [0029](0029-simulations-in-csharp.md) | Simulations in C#, called evals: one harness for checks, regressions and demos (amended 2026-10-24: the word is *eval*) | Accepted |
| [0030](0030-build-or-adopt.md) | Build or adopt: the loop stays ours on Microsoft.Extensions.AI | Accepted |
| [0031](0031-orchestration-is-an-engine-function.md) | Orchestration is an engine function, and the orchestrator stays on core | Accepted |
| [0033](0033-what-the-agent-writes-has-a-scope.md) | What the agent writes has a scope: session, agent or shared (0032 is kept for *Sources as data*) | Accepted |
| [0034](0034-code-a-person-can-maintain.md) | Code a person can maintain: one concern per file, at most 200 lines, no code in razor | Amended by 0044 |
| [0037](0037-one-agent-api-products-get-a-front-door.md) | One agent API; products get a front door: the API, its project files and its contract name no product, and what is only a product's (its web app, compose file, cloud app, database name) lives at its own door; renames keep what is stored and linked, and `ApiNamesNoProductTests` holds it (0035 and 0036 are kept for the engine package and CL17) | Accepted |
| [0038](0038-the-browser-says-what-it-is.md) | The browser says what it is: it never disguises itself to a site; an identity names itself with `BROWSER_USER_AGENT`, never a person | Accepted |
| [0039](0039-api-routes-under-v1.md) | The agent API's routes follow one rule, under `/v1`: nouns, 201 on a create, `DELETE`, `POST /{resource}/{verb}`, the key by default, the table pinned by a test | Accepted |
| [0040](0040-records-for-values-classes-for-things.md) | Records for values, classes for things: a record holds no resource, both are sealed by default, and a closed set is an abstract record | Accepted |
| [0041](0041-json-through-records-by-hand-only-for-bytes.md) | JSON goes through records and one options object per wire; the writable DOM only for a shape decided at run time, the read-only DOM for input we don't own, the writer only when the bytes are the contract | Accepted |
| [0042](0042-the-web-shows-the-live-results.md) | The web shows the live results: a page renders what it last fetched, fetches again after any write it makes, and never keeps a count the API keeps | Accepted |
| [0043](0043-repeated-names-are-enums.md) | Repeated names are enums, one per pluggable project: a name written in more than one place (a toolset, a role, a mode) is an enum even in an open set; a pack, an identity or an eval set keeps its own copy and never references another's, the rest share one; wire names stay text, carried as `[WireName]` and parsed once by `SettingChoice` |
| [0044](0044-the-size-limit-counts-code.md) | The size limit counts code: 200 lines (tests 400) without blank and comment lines, so the comments the rules ask for never force a split | Accepted |
| [0045](0045-typed-answers-and-a-shared-contract.md) | Typed answers and a shared contract: endpoints return `TypedResults` with `Results<…>` unions, errors are the `ApiError` record, and the records live in `louis-agent.api.contracts` for the API and its clients | Accepted |
| [0046](0046-dates-are-utc.md) | Dates are UTC: stored, logged and sent as ISO 8601 UTC; a screen converts them for the reader; a future local event keeps its local time and zone | Accepted |
| [0047](0047-protocol-v2-one-event-contract.md) | Protocol v2: one event contract both sides share: the runner and the API read and write the records in `louis-agent.evals.protocol`, JSON Lines with `"event"` first, kebab-case words, UTC `t`; our own field names, not OpenTelemetry's `gen_ai.*`; amends 0022 | Accepted |
| [0048](0048-mongo-through-records.md) | MongoDB goes through records: each collection a typed `IMongoCollection<T>` over a document record with its stored names as attributes, enums as wire names, dates in UTC, typed filters; `BsonDocument` only for a shape decided at run time | Accepted |
| [0049](0049-nothing-is-compatible-before-a-sale.md) | Nothing is backward compatible until something is sold in production: no migration, alias, lenient reader or second parser for what we store or send between our own parts; a changed shape means delete and re-run | Accepted |

## When to write one

When a plan makes a decision that later work must follow, or that someone will one day ask "why?" about: a provider,
a protocol, a layout or safety rule, a trade-off between cost and quality. Not for a step's details (the plan keeps
those) or a bug's cause (the fix doc keeps that). The ADR lands in the same PR as the work it decides.

## Rules

- **Numbered** `NNNN-title-in-kebab-case.md`, in order; numbers are never reused.
- **Never edited once accepted**, except its *Status* line. To change a decision, write a new ADR that supersedes it,
  and set the old one's status to *Superseded by NNNN*, linking the new record.
- **Amended** when a later ADR changes part of a decision and the rest stands: the old one's status becomes *Amended by
  NNNN*, linking the new record.
- **Short:** a page at most. The detail stays in the feature, fix, plan or spec it links to.

## Template

```markdown
# NNNN. Title: the decision, in a few words

**Status:** Proposed | Accepted | Superseded by [NNNN](NNNN-….md) | Amended by [NNNN](NNNN-….md) · **Date:** YYYY-MM-DD (decided) ·
**Recorded:** YYYY-MM-DD

## Context

What forced a choice: the problem, the constraints, the options considered.

## Decision

What we do, stated as a rule ("We …").

## Consequences

What gets easier, what gets harder, and what we now have to keep doing.

## Links

The feature, fix, plan or spec where it was decided.
```

---
[Docs index](../README.md) · Related: [Architecture](../architecture/README.md) · [Plans](../plans/README.md)
