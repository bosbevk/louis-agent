# LouisAgent.Evals.Contracts — design

> **Kind:** library (`net10.0`) · **Path:** `src/evals/LouisAgent.Evals.Contracts/` · **References:** none ·
> **Used by:** LouisAgent.Evals, LouisAgent.Api.Evals

![The records a run is made of: what happened in a turn, its marks, the run with its conversations and repeats, and the API's rows of them](diagrams/LouisAgent.Evals.Contracts.svg)

## Purpose

This project is the shape of a run of an eval. The evals write a run, the eval routes send it, and a page in the
browser will draw it, and all three compile against these records. So a field renamed here breaks every reader at
build time, instead of a page quietly showing nothing. It is the shared contract the API and the accounts already
have ([ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md)).

It references no project and no package of ours, which is why it exists apart. A browser project can't reference
[LouisAgent.Evals](LouisAgent.Evals.md), since that stands on the engine, but it can take these records.

The doc tells the records from the smallest up, what happened in a turn, its marks, then the run, and then how one
record travels from its file to a page.

## How it works

### 1. What happened in a turn, and its marks

A turn as the agent did it is a `TurnTranscript`: what was asked, the answer as the agent sent it, each tool call as
a `ToolCallTranscript` with its input and its result as the agent got it, whether that call failed, how long the
turn took, and its error if it had one. Each fact and each criterion ends as a `Mark`: its words, met or not, and one
plain line of why. A `TurnRun` puts them together: the transcript, a mark for each fact and each criterion in the
file's order, and what the agent's turn cost with its currency. A cost nobody could price is null, never 0
([ADR 0007](../adr/0007-measure-every-request.md)).

![A run's records: a run holds its conversations, a conversation its repeats, a repeat its turns, and a turn its transcript and its marks](diagrams/LouisAgent.Evals.Contracts-records.svg)

### 2. A run, and whether it passed

A `RepeatRun` is one play of a conversation, in a session of its own: its number from 1 and its turns. A
`ConversationRun` is every play of one conversation: its id, how many repeats the run asked for, and the repeats it
holds. An `EvalRun` is the run itself: its id, the eval's name, the agent's address, its kind, how many repeats it
asks, when it started, its conversations, where it is, when it ended, why it couldn't finish, which live run it
replays, and whether the visitor's page was sent with each ask, which no run does yet. Dates are UTC
([ADR 0046](../adr/0046-dates-are-utc.md)).

Whether something passed is asked of the record and worked out nowhere else
([ADR 0123](../adr/0123-ask-the-type.md)). A repeat passed when every mark of every turn is met. A conversation
passed when it holds every repeat the run asked for and each one passed, so a run that stopped part way never counts
it. The run's `Passed` is how many of its conversations passed. Each is a property, so it is written into the run's
file and a page reads it with no sum of its own.

`RunKind` says live or replay, and `RunStatus` running, finished or could not finish. Their wire names, `live`,
`replay`, `running`, `finished` and `could-not-finish`, are .NET's own `JsonStringEnumMemberName`, which `[WireName]`
is underneath. So the project still references nothing, and `.Wire()` and `SettingChoice` read the same names
([ADR 0024](../adr/0024-choices-are-enums.md)).

### 3. The API's rows

The eval routes list in short what a page shows in a list. An `EvalRow` is one eval: its name, its agent, how many
conversations, and its last run. A `RunRow` is a run in short, made from the run with `RunRow.Of`: its id, kind,
status, repeats, when it started and ended, how many conversations passed and how many it graded. `EvalsReply`
holds the rows of every eval, and `StartRunRequest` is what a start asks for: live or replay, and one repeat or
three.

### 4. One record, from its file to a page

The runner fills a run, the store writes it as the run's file, and the eval routes send what was kept, in the same
JSON. So the file is the record, and what a person opens in Rider is what the API answers.

![A run's record, from its file to a page: the runner fills it, the store writes it, the API sends what was kept, and a page reads the same shape](diagrams/LouisAgent.Evals.Contracts-flow.svg)

![Who writes and who reads a run: the runner saves it, the routes find it in the store and answer with it](diagrams/LouisAgent.Evals.Contracts-sequence.svg)

*Decided in:* F126.3.1, [ADR 0045](../adr/0045-typed-answers-and-a-shared-contract.md),
[ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

## Structure

```
src/evals/LouisAgent.Evals.Contracts/
├── Grading/        # Mark: a fact's or a criterion's mark
├── Transcripts/    # TurnTranscript and ToolCallTranscript: what happened in a turn
├── Runs/           # EvalRun, ConversationRun, RepeatRun, TurnRun; RunKind and RunStatus
└── Api/            # EvalRow, RunRow, EvalsReply, StartRunRequest: what the eval routes send and read
```

## Configuration

None. A run's JSON options are the evals' `RunJson`, in [LouisAgent.Evals](LouisAgent.Evals.md).

## Tests

It has no test project of its own: its records are held where they are used. The pass rule is
`RunRecordsTests` and a run's file read back whole is `RunStoreTests`, both in `LouisAgent.Evals.Tests`; the routes'
rows are `EvalRoutesTests` in `LouisAgent.Api.Evals.Tests`.

## Limits and plans

No page reads these records yet, and the project is not a package
([ADR 0130](../adr/0130-the-framework-is-published-as-packages-one-tag-each.md)): no product outside this repository
runs an eval yet.

## Related docs

[LouisAgent.Evals](LouisAgent.Evals.md) · [LouisAgent.Api.Evals](LouisAgent.Api.Evals.md) ·
[ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md) · [Test projects](tests.md)

---
[Projects](README.md) · Previous: [LouisAgent.Evals](LouisAgent.Evals.md) · Next: [LouisAgent.Api.Evals](LouisAgent.Api.Evals.md)
