# 0012. One orchestrator per microservice, which asks louis-agent for fixes

**Status:** Accepted · **Date:** 2026-10-06 · **Recorded:** 2026-10-14

## Context

A service's errors should be triaged by an agent that knows the service's rules (what is safe to fix automatically,
what goes to a person), while the code is changed by an agent that can only touch that one repository. One agent doing
both would hold the deploy and tracker credentials and the write access to the code together.

## Decision

The orchestrator (`louis-agent.orchestrator`, built on core with its own `toolsets:`) is configured per microservice:
a folder of runbooks, one per API method, selected with `ORCHESTRATOR_SERVICE`. It runs in its own container, not inside
the service it watches. It asks for a fix by opening a session on that service's louis-agent.api, and verifies the
result itself. Credentials follow the split: the orchestrator holds the tracker, deploy and business credentials;
louis-agent holds only git and build access to its one repository.

## Consequences

- A bad orchestrator configuration can ask for a fix, not push to production; the orchestrator, not louis-agent, decides
  what is merged and deployed.
- The POC ran it end to end: ten bugs, ten fix branches, each verified, merged by a person.
- **Still open** in the spec: the production topology (one shared instance or one per service), triggering by webhook,
  pull requests instead of local branches, and whether the real one keeps core or moves to Anthropic's Tool Runner.
- Branch protection on `main` is required: the token louis-agent needs for a PR branch can usually push to `main` too.

## Links

[Orchestrator design](../specs/ORCHESTRATOR_DESIGN.md#decisions-one-orchestrator-per-microservice) ·
[Runbooks](../RUNBOOKS.md) · [Orchestrator demo](../../samples/README.md)
