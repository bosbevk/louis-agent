# FX235 A replay records what its tools answered

> **Plan, for Louis's approval.** A fix of four steps after this plan: the free replay plays back each tool's
> recorded answer instead of running the tool again, so a change to the docs, or anything else a tool reads, no
> longer makes a recording stale. **Elsewhere:** the replay's design in
> [LouisAgent.Evals](../projects/LouisAgent.Evals.md); the parent it came out of, [F126.5](F126.5-new-evals.md).

![FX235 in four steps: a recorded tool, the registry that wraps every tool in it, the guide's baseline recorded again, and the close](diagrams/FX235-a-replay-records-its-tools-overview.svg)

## Context

**What broke.** The guide's free replay (`GuideEvalReplayTests`) plays a live run back with no key. It answers the
model from the recording, but it runs every tool for real. The docs tools read the docs as they are today, so when
F126.5.1's own fix rows added a line holding "build a pack" to `TODO.md`, a recorded `SearchDocs` found one more line,
the next model request no longer matched, and the replay stopped: "message 5 (tool) changed". Any edit to a doc whose
words match a recorded search does the same, and the TODO changes in almost every commit.

**What Louis picked, 2026-11-24, each on a card.**

| The choice | His pick |
|---|---|
| How the replay holds up against a docs change | "Record tool results": a live run records what each tool answered, a replay plays it back |
| Where the answer is recorded | "Decorate each tool": a decorator on each tool, wrapped where the registry takes it in |
| When a tool is played back | "Only with the model": tools play back only when the model's answers do |

**What was looked up.** The replay store (`ReplayStore`) already keeps any kind of answer under a kind and a key, in
the trial's index, so a new seam needs no new store (its own summary). The web's seams, such as
`RecordedSearchProvider`, are Decorators that ask `ReplayTrial.Current` on each call: record, replay, or pass
through. `ModelReplayChatClient` replays only when `SessionReplay.Current.Model` is true. Every tool call runs
through `ToolCallGuards.InvokeAsync`, which calls `context.Function.InvokeAsync`, and every tool enters the engine
through `ToolRegistry.Add`, made once in `AgentEngine`, which already holds the store. Microsoft.Extensions.AI 10.10
ships `DelegatingAIFunction`, a base class that passes a tool's name, description, schema and `UnderlyingMethod`
through to the tool it wraps (its own XML docs, in the package), so the guards' `EndsTurn` check still sees the
tool's method. The pattern is the Gang of Four's Decorator ([Refactoring.Guru](https://refactoring.guru/design-patterns/decorator)),
used as Fowler's [self-initializing fake](https://martinfowler.com/bliki/SelfInitializingFake.html), as the model's
replay is. When a tool throws, Microsoft's tool loop hands the model an error result instead, so a tool missing from a
recording shows up as the next model request changing, which the eval runner already reports as out of date.

What could not be confirmed before building: that a replayed answer reaches the model as the same bytes the live
tool's answer did, for a tool that returns text and one that returns a record. Step 2's test proves it, and the plan
stops there if it doesn't hold.

## Design

A tool in a recording trial is wrapped by `RecordedTool`. While the trial records, it runs the tool and saves its
answer, or its failure's message, under `ReplayKind.Tool` and a key of the tool's name and its arguments. While the
trial replays the model, it answers from the first recording of that key and never runs the tool. A recorded failure
is thrown again with its message, and a key the recording doesn't hold throws `ToolReplayMissException`, naming the
tool. Any other time it passes straight through. The registry wraps each function it is given when the engine has a
store, so every tool of every engine is covered, the subagents' too, and nothing else changes. The guards still run
around it: a cut-off call is still skipped, an oversized answer still summarised (that summary's model call is
already replayed), and every call still logged.

![The types: RecordedTool, a DelegatingAIFunction over a tool, made by the registry with the store](diagrams/FX235-a-replay-records-its-tools-interface-design.svg)

![Where it runs: the engine's registry wraps every tool; the store keeps tool answers beside the model's; the replay test plays them back](diagrams/FX235-a-replay-records-its-tools-architecture.svg)

![A replayed tool call: the model's recorded answer asks for a tool, the recorded tool answers from the store, and the next model request matches](diagrams/FX235-a-replay-records-its-tools-communication.svg)

![The objects in memory: the engine holds the registry, which holds each tool wrapped in a recorded tool over the store](diagrams/FX235-a-replay-records-its-tools-objects.svg)

**The namespace map.**

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `ReplayKind.Tool` | `src/engine/LouisAgent.Core/Sessions/Replay/Store/` | `LouisAgent.Core.Sessions.Replay.Store` | 2 |
| `RecordedTool` | `src/engine/LouisAgent.Core/Agent/Replay/` | `LouisAgent.Core.Agent.Replay` | 2 |
| `ToolReplayMissException` | the same | the same | 2 |
| `ToolKey` (the key's record) | the same | the same | 2 |
| `ToolReplayMessage`, its holes | the same | the same | 2 |
| `ToolRegistry` takes the store | `src/engine/LouisAgent.Core/Agent/` | `LouisAgent.Core.Agent` | 3 |
| `RecordedToolTests` | `src/tests/engine/LouisAgent.Core.Tests/Agent/Replay/` | mirrors | 2 |
| `ToolReplayThroughTheEngineTests` | the same | the same | 3 |
| `GuideEvalReplayTests` | `src/tests/site/LouisAgent.Site.Tests/Guide/Evals/` | unchanged | 4 |

**What is reused.** The replay store and its trial index, `ReplayTrial` and `SessionReplay` for the mode,
Microsoft's `DelegatingAIFunction`, and `MessageText` for the miss. No package is added.

## Steps

1. **This plan**, with its records: the plans' README, the TODO's and the work log's FX235 rows.
2. **A recorded tool.** `ReplayKind.Tool` and `RecordedTool`, tested first in `RecordedToolTests`: recording saves
   the answer and returns it unchanged; a replay with the model answers from the recording and never runs the tool;
   a recorded failure is thrown again with its message; a miss throws `ToolReplayMissException` naming the tool; no
   trial, and a replay with a live model, pass straight through; a text and a record come back as the same JSON the
   live tool gave.
3. **Every tool, wrapped.** `ToolRegistry` takes the store from `AgentEngine` and wraps each function in `Add`.
   Tested first in `ToolReplayThroughTheEngineTests`: an engine with a fake model records a turn that calls a tool,
   then replays it with the tool now answering something else, and the turn ends the same, the tool never run.
4. **The guide's baseline recorded again.** A live run of three repeats on Louis's go (about $0.09), each
   conversation's first repeat into `src/site/recordings/evals/` as F126.5.1 did, scanned before staging.
   `GuideEvalReplayTests` gives the guide an empty docs folder, written first and seen failing on the old
   recordings, then passing on the new: proof that the docs no longer reach a replay. The image's docs folder it
   builds today goes.
5. **The close.** The design docs of Core and the evals, the run-an-eval guide, FX235's rows done, its lesson, and
   this plan deleted.

## Verification

Each step's tests, written first. The full suite of unit tests before the pull request, with `main` merged in.
Paid, on Louis's go: step 4's live run, about $0.09. Free by hand: Replay free on the admin at `:5080` after step 4,
every ledger row at $0.

**Done when:** a free replay answers every tool from its recording, `GuideEvalReplayTests` passes over an empty
docs folder, and a docs edit can no longer turn it red.

## Concepts

| Step | What it teaches | Skill in [GOAL.md](../GOAL.md) |
|---|---|---|
| 2 | Record and replay at a seam: a Decorator as a self-initializing fake, keyed by what was asked | 18 Testing and shipping |
| 3 | One place every tool passes through, so a cross-cutting concern is added once | 4 Tool use |
| 4 | A regression replay isolated from the world it was recorded in | 12 Evaluation |

## Approved during the build

Nothing yet.

---
[Docs index](../README.md) · Related: [F126.5 New evals](F126.5-new-evals.md) · [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md) · [Plans](README.md)
