# Draft: F106 The framework's track, before the agents

> **Plan draft, not yet approved.** Written on 2026-11-07 from Louis's decision to "focus on the framework first then
> build the agents" (an hour earlier: "they must run in parallel"; the later word wins), and from a positioning page ChatGPT wrote for him (in the project files,
> `research/louis-agent-positioning-chatgpt-2026-11-07.md`). The open choices below go to Louis one at a time, three
> options each (the development guide, rules 10 and 11); when they are settled this becomes the parent plan he
> approves, and each child gets its own plan when it starts.

## Context

Until now the roadmap sold configured agents first and the engine only after Phase 10, which itself waited for every
engine feature (Louis, 2026-10-20). On 2026-11-07 Louis changed the order: **the framework first, then the agents**.
The framework's track runs now; the configured agents (the practice assistant, "get paid") are built afterwards, on
that framework, the way a developer who bought it would build them. Nothing in Phases 4 to 9 is dropped; they come
after this track's first children, and the demand check (F62) keeps running meanwhile.

What changes is *when* the framework work starts, not what the engine is. The split ChatGPT describes (the engine is
how, an identity is who, a pack is what, a composition root brings them together) is the split we already have
(ADR 0027, ADR 0076, ADR 0092).

## The positioning

- **In one line:** louis-agent is the application framework for AI agents in .NET. Build, extend, evaluate and run
  agents on a modular architecture with reusable packs, without assembling the infrastructure yourself.
- **The test for every framework feature:** does this make it easier for a .NET developer to build, extend, test or
  operate an agent application? If it only adds an abstraction, it needs a stronger reason (ADR 0055, keep it simple).
- **What we don't claim:** that we are the only framework with provider choice, local models, tools, multi-agent,
  telemetry or evals. Others have them. The claim is the whole path being simpler and more coherent, and that has to
  be shown, not said.
- **The comparison that matters** is Microsoft Agent Framework, because it targets the same .NET developers. The
  answer is a measured side-by-side, not a feature list.
- **"ABP for AI"** (ABP is a known .NET application framework with modules and conventions) describes the direction
  well, but leans on another product's name. Whether it is used in public is an open choice below.

## How it compares, and what stays

Louis asked ChatGPT how Microsoft Agent Framework and LangGraph organise the same job (2026-11-07). Its reading:
Microsoft centres on an *agent* (model, instructions, tools, a session) with *workflows* that connect agents and
functions; LangGraph centres on a *graph* (state, nodes, edges, checkpoints) that controls how work moves. Both give a
developer building blocks for executing agents and workflows; ours gives a way to package and compose whole agent
applications: the engine is how, an identity is who, a pack is what, and a pack is written once and used by several
identities. That split stays (ADR 0027); it is a convention the others could follow but don't make foundational,
and it is the scenario F106.4's side-by-side should build: one shopping or recipes pack used by two identities, in
both frameworks.

What that reading changes here:

- **"Workflow" as a fourth concept** is not added now. Orchestration is already planned as an engine function with
  typed results checked in code (Phase 6, F39, ADR 0031); F39's plan decides whether a workflow is a named thing a
  developer composes or stays a delegation the engine runs. Until it is measured, three concepts.
- **What to study in Microsoft's framework** before F106.4 (its own docs, not a summary): its agent loop, session state
  and persistence, middleware pipeline, workflow execution, human-in-the-loop, checkpointing and tool permissions.
  Each maps to a phase of ours (F43, F41, F42, F39), so the comparison also tells us where our phases stand.

## Where we are and where the API should get to

Today a host composes by hand: it builds an `AgentCatalog` and `HostServices`, calls `AgentHost.Build`, and hands the
result to `AddLouisAgentApi` (see `demos/starter/hosts/louis-agent.starter-api/Program.cs`). The packs a demo uses are
named in one place (`demos/puls/hosts/louis-agent.host-packs`). It works, but a newcomer has to know four types before
their first agent answers.

The direction ChatGPT sketched, as an illustration only (no such API exists yet):

```csharp
var builder = AgentApplication.CreateBuilder(args);
builder.AddOllama("qwen3");
builder.AddPack<CalculatorPack>();
builder.AddIdentity<MyAssistant>();
await builder.Build().RunAsync();
```

This is roughly the registration API the Phase 10 draft already asks for (`AgentCatalog.Add`), pulled forward.

## Proposed children

Each its own plan, branch and pull request, about ten steps at most; the order is the first open choice.

| Child | What | Done when (proposed) |
|---|---|---|
| F106.1 | A builder for the common path: providers, packs and an identity registered in a few lines, on top of today's `AgentHost.Build`, which stays as the escape hatch | The starter's API program composes through it, and a test builds an agent with one pack in under ten lines |
| F106.2 | The framework's own docs: "your first agent in ten minutes", then "write a pack" and "write an identity", on the engine's site | Someone who isn't Louis follows the first guide to a working agent, timed |
| F106.3 | A second example outside Puls: a small agent of another kind (for example the practice's invoice clerk from Phase 4), built only from the framework's public parts | It builds without referencing any Puls project, and an eval proves it |
| F106.4 | The side-by-side: the same small application built in louis-agent and in Microsoft Agent Framework, same model, tools, delegation case and eval set | A results page compares lines of code, setup, time to build, eval scores, cost, and what debugging was like; a paid run only on Louis's go |
| F74 | Packaging in place (the Phase 10 draft's Stage A): public surface declared, reproducible build, a package and a consumer test built from it | Moves into this track; Stage B (its own repo) stays after the engine features |

## Open choices, one at a time

1. **What the track builds first** (F106.1, F106.2, F106.3 or F106.4).
2. **The shape of the builder** (its pattern: a builder of its own like `WebApplication.CreateBuilder`, extension
   methods on the .NET generic host, or a fluent sub-builder like `AddIdentityCore`).
3. **"ABP for AI" in public or only as an internal compass.**
4. **When packaging (F74's Stage A) starts**, now in this track or after F106.1.

## Concepts

Framework design and developer experience (skill 18, testing and shipping: a package a stranger can use), the
composition root and dependency injection, and measuring a framework against another (skill 12, evaluation, applied
to the framework itself rather than an agent).
