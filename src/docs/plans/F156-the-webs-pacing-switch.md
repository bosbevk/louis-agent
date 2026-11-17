# F156 The web's pacing switch

A plan: how the page learns that the server already paced a turn's words and leaves its own pacer off, and how
louis-agent.dev turns the engine's pacing on, so its guide's words are paced once, by the server. It has six steps.

## Context

F155 gave the engine a switch, `STREAM_PACE`, that paces a turn's text on its way out. Louis settled the layers
then: "web and engine no API", a switch in the engine and one on the page, and of pacing on every layer, "is that
layered approach too much": yes, since each pacer holds the text back to keep its rhythm, so pacing twice only adds
lag. F156 is the page's side. Of three ways for the page to know the server paces, he picked A: the API says so with
each turn.

The code was read for what is there. The guide's caption (`AgentCaption`) paces whenever its answer was still
arriving when it first drew (`_paced = Streaming`), and has no way to know whether the server paced it. The API's
`SessionHandlers.SendMessage` streams a turn as server-sent events and holds the `HttpContext`, and it can read the
engine's settings through `host.Agent`, so it knows whether `STREAM_PACE` is on. The shared contract already names
one header, `ApiHeaders.RequestedWith`. On the page, `AgentApi.StreamMessageAsync` reads the response's headers before
its events, and `ChatRunner` applies each event to the turn through `TurnStream.Apply`. The stream's events copy
Anthropic's own format, so a new event would break that copy; a header leaves the events as they are. The API sets
up no CORS today, so a page on the same site reads the header as it is; a page on another site would need the API to
expose it, which waits for the day the API serves one. The site's API takes its settings from its compose file
locally (`src/site/docker/docker-compose.yml`) and from `src/site/deploy/azure/louis-agent-app.yaml` in Azure.

## Design

![The site's API turns the engine's pacing on and says so on each turn; the page paces only when the server did not](diagrams/F156-the-webs-pacing-switch-architecture.svg)

### 1. The API says so

When `STREAM_PACE` is on, the API answers each turn with the header `Stream-Paced: on`, named once in the shared
contract as `ApiHeaders.StreamPaced`. Without it, there is no header, and a page paces as it does today. The header
comes with the response, before the first event, so the page knows before the first word.

![The interface: the header in the contract, the API that sets it, the page that reads it](diagrams/F156-the-webs-pacing-switch-interface-design.svg)

### 2. The page reads it, and its pacer stays off

`AgentApi.StreamMessageAsync` takes a callback, `onServerPaced`, and calls it once when the header says on. The
chat runner's callback marks the turn `ServerPaced`. The bar hands the caption its turn's `ServerPaced`, and the
caption paces only when its answer is still arriving and the server did not pace it: `_paced = Streaming &&
!ServerPaced`. Everything else about the caption, its growing in and its fading, is as it was.

![One turn on louis-agent.dev: the header before the first word, so the caption knows before it draws](diagrams/F156-the-webs-pacing-switch-communication-one-turn.svg)

![The objects of one turn: the turn carries whether its server paced it](diagrams/F156-the-webs-pacing-switch-objects-in-memory-one-turn.svg)

### 3. louis-agent.dev turns it on

`STREAM_PACE: "on"` in the site's compose file and in its Azure app's settings. The code-review demo's API and Puls
keep it off, so their pages pace as now.

### 4. The namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `ApiHeaders.StreamPaced`, `StreamPacedOn` (added) | `src/hosts/louis-agent.api.contracts/` | unchanged | 2 |
| `SessionHandlers.SendMessage` (sets the header) | `src/hosts/louis-agent.api/Endpoints/Sessions/` | unchanged | 2 |
| `SessionHandlersTests` (the header, on and off) | `demos/puls/tests/louis-agent.api.tests/Endpoints/` | unchanged | 2 |
| `AgentApi.StreamMessageAsync` (`onServerPaced`) | `src/web/louis-agent.web/Api/` | unchanged | 3 |
| `Turn.ServerPaced`, `ChatRunner` | `src/web/louis-agent.web/Chats/` | unchanged | 3 |
| `AgentApi`'s and the runner's tests | `src/tests/web/louis-agent.web.tests/Api/`, `Chats/` | unchanged | 3 |
| `AgentCaption.ServerPaced`, `AgentBar` | `src/web/louis-agent.web/Components/Widget/Bar/` | unchanged | 4 |
| `CaptionMotionTests` (no pacing when server paced) | `src/tests/web/louis-agent.web.tests/Components/Widget/Bar/Caption/Motion/` | unchanged | 4 |
| `docker-compose.yml`, `louis-agent-app.yaml` | `src/site/docker/`, `src/site/deploy/azure/` | — | 5 |

### 5. What is reused

The engine's `STREAM_PACE` (F155), the contract's `ApiHeaders`, the response the page already reads its events from,
and the caption's existing switch. No new package, and no change to the stream's events.

## Steps

Each step is one commit with its test, written first and seen failing on an assertion. Stop after each for Louis's
review, showing the commit before it is made.

### Step 1: the plan is kept

A docs-only step. This plan and its pictures are committed, with its rows in the plans' index, the TODO and the
work log's open table.

### Step 2: the API says so

The test comes first. `SessionHandlersTests` sends a message with `STREAM_PACE` on and holds the response's
`Stream-Paced: on`; with it off, no such header. Then the header's name in `ApiHeaders` and the line in `SendMessage`.

### Step 3: the page reads it

The test comes first. `AgentApi` over a stub that answers with the header calls `onServerPaced` once, and without
the header never; the chat runner marks its turn `ServerPaced`. Then the callback and `Turn.ServerPaced`.

### Step 4: the caption's pacer stays off when the server paced

The test comes first. A caption drawn while its answer streams, with `ServerPaced`, shows the words as they come,
whole, where without it it paces them. Then the parameter, the caption's one line, and the bar handing it on.

### Step 5: louis-agent.dev turns it on

`STREAM_PACE: "on"` in the site's compose file and its Azure app. By eye on :5080: a question to the guide, the
caption's words sampled as they arrive, even and with no second pacing, its cost reported (a small paid call).

### Step 6: the docs, and the pull request

The web's design doc says when the caption paces and when the server does, the API's says what the header means,
and SETUP says `STREAM_PACE` is on for louis-agent.dev. The write-up is written, this plan is folded and deleted,
and the records move to done. `main` is merged in, the full unit suite passes, and the pull request is opened on
Louis's word.

## Verification

The tests named in each step, then the full unit suite. By hand on :5080, step 5's one question to the guide, a
small paid call whose cost is reported: the words arriving evenly, paced once.

## Concepts

Step 2 is the one to study. When two parts can each do the same job, they need one source of truth for which does
it, or they drift: here the server says what it did, on every answer, and the page follows. A header carries that
beside the stream without changing the stream's own format. That is protocols, skill 11, in
[the goal](../GOAL.md).

## Approved during the build

---
[Docs index](../README.md) · Related: [Plans](README.md), [louis-agent.web](../projects/louis-agent.web.md),
[louis-agent.api](../projects/louis-agent.api.md)
