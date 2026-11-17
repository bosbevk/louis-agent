# F155 Pacing in the engine

A plan: how the engine hands a turn's text out at a steady pace, so every host that asks for it gets the words
evenly instead of in the model's bursts. It has six steps.

## Context

Louis, 2026-11-17, after F154 paced the guide's words on the page: "can't we also fix API streaming to be more
timed", "and in the engine on 3 layers", then "note engine pacing and api pacing". Of three places for the
engine's pacing he picked A: pace the turn's stream on its way out, a decorator over what the engine hands a host,
which a host turns on with a setting. Then, asked to research the best way first, he settled the layers: "ours
should also be a switch", and of pacing in all three layers, "is that layered approach too much": yes, since each
pacer holds the text back to keep its rhythm, so pacing twice only adds lag. So "web and engine no API": the words
are paced once per path, by the engine's switch or the page's, never both. The API needs no pacer of its own, since
it is a host of the engine: F156 becomes the web's switch, its pacer off when the server paces, and the API's host
turning the engine's on. And "web should change to also continuous": it does, by sharing the one rule.

The research: the pattern every source uses is a buffer between the model's arrival and what is shown, drained at
a pace of its own. The Vercel AI SDK ships it on the server as `smoothStream`, which releases text word by word and
passes tool calls and other parts through at once ([its reference](https://ai-sdk.dev/docs/reference/ai-sdk-core/smooth-stream)).
A fixed typing speed either falls behind a fast model or shows the bursts again; the better rule is continuous,
the larger of a base pace and the backlog over a short catch-up window, and it reveals whole words or characters,
never part of one ([Smoothing streamed LLM text](https://dev.to/mzsleepyzzz/smoothing-streamed-llm-text-in-react-it-looks-like-a-typewriter-loop-it-isnt-eb3)).
`smoothStream`'s docs don't say what becomes of waiting text when a tool call arrives; this plan sends it first, so
the order never changes, and tests it.

The code was read for what is there. The engine streams one turn through `AgentEngine.StreamPromptAsync`, which
returns `TurnRunner.StreamAsync` as it is. Three hosts read it: the API (`Sessions/TurnStream`), Rider through ACP
(`Turns/TurnRelay`) and the CLI (`ConsoleChat`). Inside the turn, `ChatPipeline` runs the tool loop, records usage
and traces each request, and `TurnRunner` appends the finished turn to the history whole (ADR 0008). Nothing on
the way paces or batches the text: each piece the model sends goes straight on. The evals also run their turns
through the API, so anything that slowed the stream for everyone would slow them.

The rule of how fast words should show already exists: `WordPace`, in the web, which F154 wrote for the guide's
caption. It is plain arithmetic: about 90 letters a second, a step faster once more than 240 letters behind, faster
again once the answer is done. Both the engine and the web already reference `louis-agent.session-stream`, so the
rule can move there and be one rule for both layers, instead of a copy per layer, and become continuous on the
way, which the caption then gets too.

The engine already takes a clock (`TimeProvider`, its constructor's `clock`), and the core tests already have the
fake one (`Microsoft.Extensions.TimeProvider.Testing`), so the pacing is tested in fake time, with no new package. A
host's settings reach the engine through `AgentOptions`, read from the environment, as `USAGE_IN_CHAT` is.

## Design

### 1. Where the pacing sits

![The pacing sits in the engine after the turn, so the model call, the tool loop, the history and the usage records run as fast as today](diagrams/F155-pacing-in-the-engine-architecture.svg)

Outside the turn, never inside it. The model call, the tool loop, the usage records, the traces and the history
all happen exactly as today, at the model's speed. Only what a host reads is paced. A host that leaves the setting
off, as the evals do, gets the stream as it is now.

### 2. The paced stream

![The engine's turn stream passes, when a host turns it on, through a paced stream that hands out its text at WordPace's pace](diagrams/F155-pacing-in-the-engine-interface-design.svg)

`PacedStream` is a Decorator over the turn's stream (an Iterator over an Iterator): the same updates come out, in
the same order, only the text spread over time. Inside it, a reader task takes the turn's updates as fast as they
come and writes them into a channel, so the turn never waits for the pacing. The writer side keeps the text not yet
handed out, and about thirty times a second asks `WordPace` how many letters may show, and hands out the whole words
that fit in them, as one text update: never part of a word, nor half of a character or of a markdown mark. Only the
text before a tool call and the answer's last letters go out whole, words or not. An update that is not text, a tool call, a tool's result, the thinking, the usage or the end, goes out as it
is once the text before it has: so the order never changes, and the text before it finishes at the finishing pace.
When the turn ends, the last letters go out at the finishing pace, about a second and a quarter for a couple of
hundred letters. A cancelled turn stops the reader and the timer at once, and an error in the turn reaches the host
after the text before it.

![One paced turn: the reader takes the turn as fast as it comes, the writer hands the text out at WordPace's pace](diagrams/F155-pacing-in-the-engine-communication-one-paced-turn.svg)

![The objects of one paced turn: a channel the reader fills, the text not yet handed out, and a timer on the engine's clock](diagrams/F155-pacing-in-the-engine-objects-in-memory-one-paced-turn.svg)

### 3. The setting

`STREAM_PACE`, read into `AgentOptions.StreamPace`: on paces the text, off, the default, leaves the stream as
today. A bool, as `USAGE_IN_CHAT` is. It is one of two switches, the page's pacer the other, and on one path only
one is on. F156 turns this one on for the API's host and the page's off when the server paces.

### 4. The rule, continuous

`WordPace.Next` today is a step: 90 letters a second, then a faster pace once more than 240 letters behind, and a
finishing pace once done. It becomes continuous: the larger of the steady 90 letters a second and the backlog over a
short catch-up window, and once done, the finishing pace as now. So it never falls behind a fast model and never
lurches at a threshold. The window keeps the caught-up feel F154 has: a backlog of 240 letters still clears in
about the same time as now. The caption on the page reads the same rule, so it gets smoother with nothing of its
own to change.

### 5. The namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `WordPace` (moved) | `src/engine/louis-agent.session-stream/Pacing/` | `LouisAgent.SessionStream.Pacing` | 2 |
| `WordPaceTests` (moved) | `src/tests/engine/louis-agent.session-stream.tests/Pacing/` | `LouisAgent.SessionStream.Tests.Pacing` | 2 |
| `AgentCaption` (its using) | `src/web/louis-agent.web/Components/Widget/Bar/Caption/` | unchanged | 2 |
| `WordPace.Next` (continuous) | `src/engine/louis-agent.session-stream/Pacing/` | `LouisAgent.SessionStream.Pacing` | 3 |
| `PacedStream` | `src/engine/louis-agent.core/Agent/Turns/Pacing/` | `LouisAgent.Core.Agent.Turns.Pacing` | 4 |
| `PacedStreamTests` | `src/tests/engine/louis-agent.core.tests/Agent/Turns/Pacing/` | `LouisAgent.Core.Tests.Agent.Turns.Pacing` | 4 |
| `AgentOptions.StreamPace` (added) | `src/engine/louis-agent.core/Config/` | unchanged | 5 |
| `AgentEngine` (paces when on) | `src/engine/louis-agent.core/Agent/` | unchanged | 5 |
| `AgentEnginePacingTests` | `src/tests/engine/louis-agent.core.tests/Agent/Turns/Pacing/` | `LouisAgent.Core.Tests.Agent.Turns.Pacing` | 5 |

### 6. What is reused

`WordPace` is the pace, moved, not written again. `System.Threading.Channels` and `PeriodicTimer` on the engine's
`TimeProvider` carry the reader and the tick, both in .NET. The tests use `FakeTimeProvider`, which the core tests
already have. No new package.

## Steps

Each step is one commit with its test, written first and seen failing on an assertion, except step 2, a move whose
tests move with it and pass unchanged. Stop after each for Louis's review. Six steps.

### Step 1: the plan is kept

A docs-only step. This plan and its pictures are committed, with its rows in the plans' index, the TODO and the
work log's open table.

### Step 2: the rule moves where every layer can read it

`WordPace` and its tests move from the web to `louis-agent.session-stream` and its tests, in a `Pacing` folder,
unchanged but for their namespace. The caption uses it from there. The web's tests and the session stream's pass
as they did. A pure move.

### Step 3: the rule, continuous

The test comes first. `WordPaceTests` gains: behind by any amount, the pace is the larger of the steady pace and the
backlog over the catch-up window, with no step at 240 letters; 240 letters behind still clear in about the time they
do now; the finishing pace is unchanged. Then `WordPace.Next`. The caption's own tests run against it and pass.

### Step 4: the paced stream

The test comes first. `PacedStreamTests` runs a fake turn on a `FakeTimeProvider` and holds: a burst of 300 letters
comes out over time at the pace, not at once; each piece handed out ends on a whole word, never part of one or half of
a character; a tool call waits for the text before it, which goes out whole, then the call goes out as it is; the
order of the updates is the turn's; once the turn ends the rest finishes at the finishing pace; a cancelled turn
stops the reader; an error in the turn reaches the reader after the text before it.

Then `PacedStream`.

### Step 5: the engine paces when a host asks

The test comes first. `AgentEnginePacingTests` builds an engine on the shared fake model with `StreamPace` on and a
fake clock, and holds that a turn's text comes out paced; with it off, the stream is the turn's as today; and the
history after the turn holds the whole answer either way. `AgentOptions` reads `STREAM_PACE`.

Then the setting and `StreamPromptAsync`'s one branch.

### Step 6: the docs, and the pull request

The core's design doc says where the pacing sits and why there, and the settings guide names `STREAM_PACE`. The
write-up is written, this plan is folded and deleted, and the records move to done. By hand: the CLI with
`STREAM_PACE=on` and a small question, its text seen arriving evenly. `main` is merged in, the full unit suite
passes, and the pull request is opened on Louis's word.

## Verification

The tests named in each step, then the full unit suite. By hand, step 5's one small question through the CLI, a
paid call whose cost is reported. Nothing on the live site changes in F155: no host turns the setting on until
F156.

## Concepts

Step 3 is the one to study. Streaming shows the model's words as they come, but the model sends them in bursts. A
paced stream is a producer and a consumer at different speeds with a buffer between them: the reader keeps up with
the model so the turn is never held back, and the writer keeps a reader's pace, catching up when it falls behind.
Where it sits matters as much as how it works: after the turn, so the agent's work, its cost records and its
traces stay true. That is the agent loop, skill 6, and testing in fake time is testing and shipping, skill 18,
both in [the goal](../GOAL.md).

## Approved during the build

- **2026-11-17, step 4: method names have a rule, ADR 0126.** Of `PacedStream.Of(turn, clock)`, Louis asked "this Of
  method is this a valid naming pattern?", picked A of three, an extension named for what it gives back, and asked
  for "method name rules and ADR when to use what naming convention", applied "on all touched files"; of the
  proposal, "yes with the test-builder exception". So the paced stream is `turn.Paced(clock)`, `WordPace.Next` is
  `WordPace.Advance` and `WordPace.Letters` is `WordPace.CountLetters`; the steps and pictures above keep the old
  names as they were approved.

---
[Docs index](../README.md) · Related: [Plans](README.md), [louis-agent.core](../projects/louis-agent.core.md)
