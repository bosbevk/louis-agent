# 0023. A copyleft service may run beside us, unchanged; what we link stays MIT, Apache or BSD

**Status:** Accepted · **Date:** 2026-10-17 (decided in the F32 plan) · **Recorded:** 2026-10-17

## Context

Our rule for reuse names MIT, Apache or BSD licences for what we take in, and our own code is all rights reserved
(ADR 0016). Puls's endpoint is sold. F32 needs a search engine we run ourselves, and the one that fits, SearXNG, is
AGPL-3.0: whoever **modifies** it and lets people use it over a network must offer them the modified source. MongoDB
(F31) is already a service under SSPL, which forbids offering MongoDB **itself** as a service to others. Without a
rule, each such service is argued afresh, or avoided when it is the right tool.

A licence is not the only term that binds: SearXNG has no index of its own, it queries Google, Bing, DuckDuckGo and
others, whose terms forbid automated querying, more so for a product that is sold.

Considered: only permissive software, services included (no SearXNG); copyleft allowed anywhere (a copyleft package
linked into our code would bind our code to its licence).

## Decision

We tell **linking** apart from **running beside**.

- **Linked** (a NuGet package, a library in our process, code copied in): MIT, Apache or BSD only, as before.
- **Run beside us** (a separate container we talk to over a network protocol, HTTP or a database's wire protocol): a
  copyleft licence (AGPL, SSPL) is allowed when we run the published image **unchanged**, configured only through its
  own settings, and never sell **the service itself** (a search API, a database service). Used inside a product we
  sell, such as Puls's answers, it is allowed on those terms.
- **What the service fetches has terms of its own.** A service that scrapes others' results (SearXNG) serves
  development, tests and benchmarks; the sold endpoint uses a provider whose terms license its results (Tavily, Brave).
  *Recorded 2026-10-18 (F34):* a shop's catalogue is read as a shopper's page is (F33: no internal API called
  directly), with one approved exception, Checkers' store lookup: our script makes the call its own address picker
  makes (`/api/store/fetch-store-contexts`, the town's centre only), once per town a day. The pause after Checkers
  refuses a search (`ShopThrottle`, 20 s) backs off from its rate limit; it never works around one.
- The plan that adds such a service names its licence, the terms of what it fetches, and why both fit; a change to its
  code, or selling it as itself, is a new decision.

## Consequences

- SearXNG (F32, for development and benchmarks) and MongoDB (F31, benchmark results) fit; the next such service is a
  line in its plan, not a debate.
- We can't patch such a service; a fix goes upstream or we choose another tool.
- The sold endpoint keeps a paid search API, so its search costs money; a benchmark on SearXNG measures the agent, not
  the production search.
- This is the project's reading of the licences, not legal advice: before customers depend on a service beside Puls,
  someone qualified checks it.

## Links

[F32 plan](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/plans/F32-self-hosted-web.md) · [F32 feature](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F32-self-hosted-web.md) ·
[ADR 0016](0016-licence-all-rights-reserved.md) · [ADR 0022](0022-benchmarks-recorded-from-the-protocol.md)
