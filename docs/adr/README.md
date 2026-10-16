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
| [0012](0012-one-orchestrator-per-microservice.md) | One orchestrator per microservice, which asks louis-agent for fixes | Accepted |
| [0013](0013-documentation-structure.md) | Documentation: one type per doc, guides by folder, arc42 and ADRs | Accepted |
| [0014](0014-test-first.md) | Test first: a step's tests are written and seen failing before the code | Accepted |
| [0015](0015-licence-agpl.md) | Licence: AGPL-3.0, so improvements to a hosted Puls stay open | Superseded by 0016 |
| [0016](0016-licence-all-rights-reserved.md) | Licence: all rights reserved | Accepted |
| [0017](0017-skills-by-level.md) | Skill files follow the levels: a folder per level, and a role opens only at its own | Accepted |
| [0018](0018-built-in-prompts-embedded.md) | Built-in prompts are embedded, read only from the assembly | Accepted |

## When to write one

When a plan makes a decision that later work must follow, or that someone will one day ask "why?" about: a provider,
a protocol, a layout or safety rule, a trade-off between cost and quality. Not for a step's details (the plan keeps
those) or a bug's cause (the fix doc keeps that). The ADR lands in the same PR as the work it decides.

## Rules

- **Numbered** `NNNN-title-in-kebab-case.md`, in order; numbers are never reused.
- **Never edited once accepted**, except its *Status* line. To change a decision, write a new ADR that supersedes it,
  and set the old one's status to *Superseded by NNNN*, linking the new record.
- **Short:** a page at most. The detail stays in the feature, fix, plan or spec it links to.

## Template

```markdown
# NNNN. Title: the decision, in a few words

**Status:** Proposed | Accepted | Superseded by [NNNN](NNNN-….md) · **Date:** YYYY-MM-DD (decided) ·
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
