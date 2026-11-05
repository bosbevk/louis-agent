# 0076. Core ships no agent, and every tool the model asks for is a pack

**Status:** Accepted · **Date:** 2026-11-05 (Louis: "the coding agent is also just a demo, so it should
be a pack identity", then pick C, every tool in core becomes a pack) · **Recorded:** 2026-11-05

## Context

Core read as "the engine", but it hid an agent. The coding agent's voice was in its prompts and was the default for any
session that named none, even a Puls one. Its file, git, dotnet, shell, web, Paymo and DevOps tools were core's. So the
framework shipped one of its own demos, and someone taking the engine as a package took all of that with it.

## Decision

- **Who runs on the engine is a demo's choice.** The coding agent is an identity in `demos/louis-agent/identity`, and its
  catalogue names its packs. A host with no identity has no default voice: a session names its own, or runs on the
  principles and its guides. The default voice comes from an identity (`IAgentIdentity.DefaultVoice`).
- **A tool the model asks for is a pack:** `files`, `git`, `dotnet`, `shell`, `web`, `paymo`, `devops`. A pack sees only the
  composition, and one installs as a NuGet package with no engine in its dependency graph; a test proves it.
- **What every pack relies on stays in the engine**, and a pack gets it through `IPackContext`:
  - the browser, search, page renderer and recordings, and the guards around each fetch (no internal addresses,
    `robots.txt`). They live in `Web/`. A pack asks for a fetcher, so it can't leave a guard out.
- **What needs the engine's own insides stays too**, because a pack can't reach them:
  - delegation (`Delegation/`), which opens sessions and rolls up their usage: orchestration is an engine function
    ([ADR 0031](0031-orchestration-is-an-engine-function.md));
  - the skill tools and agent-built tools, which change the engine's own registry; the clock, which every session gets.
- **What both sides need is in the composition:** running a script (`PythonRunner` and its two siblings), since the engine
  runs skills whether or not a session has the toolset; the page readers; the shared texts.
- **The order of a session's tools is fixed:** general tools, the engine's, then a product's. It is part of the prefix the
  model's cache keys on, so a pack says which side it sits on (`ComesBeforeTheEngine`).

## Consequences

- Core still holds web access, so it still depends on PuppeteerSharp. Splitting web access into its own package is one
  mechanical move, not part of this.
- A demo must name what it loads. Puls names `web`, because it uses it; the tools it only needs for its routing eval's
  baseline come through `PACKS` in its compose file, so Azure's Puls has no shell or git tools.
- Core's tests can't use a real tool, so they run on `TestTools`, a fake pack under the real names.
- The golden files moved to the coding agent's tests, since what the model reads now depends on what a demo loads. They
  stayed byte-equal through the whole move.

## Links

[0027](0027-engine-identities-and-packs.md) · [0031](0031-orchestration-is-an-engine-function.md) ·
[0074](0074-the-folders-say-what-ships.md) · [Build a pack](../guides/build-a-pack.md)

---
[Docs index](../README.md) · Related: [ADRs](README.md)
