# 0001. Record architecture decisions

**Status:** Accepted · **Date:** 2026-10-14 · **Recorded:** 2026-10-14

## Context

The goal is to be able to explain every design decision behind louis-agent ([GOAL](../GOAL.md)). Until now the
decisions were spread over the development guide's *Rules that always apply*, the specs, the fix docs, the plans and
their *Approved during the build* sections, and the architecture doc's integration tiers. A reader (Louis explaining
the project, a reviewer, or Claude working on it) couldn't find in one place why something is the way it is, or tell
whether a decision still holds. Plans are kept as approved, but they mix a decision with the steps that built it.

## Decision

We record each lasting architecture decision as an ADR in `docs/adr/`, in Michael Nygard's format (context, decision,
consequences), numbered and never edited once accepted: a later ADR supersedes an earlier one. A plan that makes such a
decision adds its ADR in the same PR. The decisions already made are recorded afterwards (0002 onwards), each dated when
it was made and linking the docs it was taken from.

## Consequences

- One place for the *why*; the guide's rules stay one-liners that Claude reads every session, each linking its ADR.
- A superseded decision stays readable, with what replaced it.
- One more doc to keep: *Finishing a feature* and *Finishing a fix* gain a line for it.
- The early ADRs are reconstructed from the records, not from memory, so each links its sources to be checked.

## Links

[CL6 plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/CL6-arc42.md) · [Nygard, *Documenting architecture decisions*](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
