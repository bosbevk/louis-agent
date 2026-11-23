# LouisAgent.Evals — design

> **Kind:** library (`net10.0`) · **Path:** `src/evals/LouisAgent.Evals/` · **References:** LouisAgent.Core,
> LouisAgent.Evals.Contracts, LouisAgent.Api.Contracts · **Used by:** LouisAgent.Api.Evals, and its tests
> **Also shows:** `component`, `object`

![The evals' library: an eval file read and found beside its agent, a turn graded by code and a judge, a run played against the agent, and the run kept and replayed for free](diagrams/LouisAgent.Evals.svg)

## Purpose

This project is how an agent is measured. An eval is one file of conversations a person has with the agent, and
this library reads that file and grades what happened in a turn. Some things code can tell for certain, such as
whether the agent called a tool. The rest is a plain sentence, a criterion, that a model judge marks yes or no with
its reason.

It also runs an eval. A run plays every conversation of a file against the agent, over the same sessions API a
person's chat uses, grades each turn, and keeps itself as one file. A live run records the agent's answers and the
judge's as it goes, so the same run can be replayed later for nothing. A site's API serves it through
[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md), and the records of a run are
[LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md)'s, so the admin's pages read them. A conversation
that names a page is sent that page with its first ask, as a visitor's message carries it, so the agent measured
is given what production's agent is given.

The doc follows a file from its text to its marks: how it's read, how a wrong one is refused, the schema an editor
checks it against, then the facts code tells and the criteria the judge marks. Then it follows a run: a
conversation played, the repeats and when a run stops, the run kept, the evals found beside their agents, and
record and replay.

## How it works

![The evals' library stands on the engine, on its contracts and on the shared wire records, and a site's API maps it through LouisAgent.Api.Evals](diagrams/LouisAgent.Evals-component.svg)

### 1. An eval file

An eval file names one agent by its address, and lists conversations. A conversation has an id, the turns a person
takes, and the page they're on when the agent is a site's guide. A turn is what the person asks, with the facts code
will check and the criteria the judge will mark. This is the guide's first conversation:

```json
{
  "$schema": "../../../../evals/LouisAgent.Evals/eval.schema.json",
  "agent": "louis-agent-guide",
  "conversations": [
    { "id": "guide-what-is", "page": "/",
      "turns": [
        { "ask": "What is louis-agent, in one minute?",
          "facts": ["no-error", "used ReadSkill", "under 30s"],
          "criteria": ["Says what louis-agent does for a business, in plain words", "Ends with one small nudge: a page, a question or the chat"] } ] }
  ]
}
```

The file names no tools, since an agent brings its own (ADR 0117). A turn may leave out its facts or its criteria,
because a follow-up such as "Next." often has only one of them. The whole guide's file, seven conversations, is the
tests' fixture, `Files/Json/Fixtures/guide.eval.json`.

The file becomes three records, one inside the other: `EvalFile`, `Conversation` and `Turn`. A fact is a `Fact`, its
kind and its one argument. Two values are written as text and read straight into their types, each by a small
converter: the agent into an `AgentAddress`, and a fact's words into a `Fact`. So code past the reader never reads
the text again.

![An eval file is three records, one inside the other; the reader makes them from the file's text, and the schema is made from the same records](diagrams/LouisAgent.Evals-interface.svg)

### 2. Reading a file, and refusing a wrong one

`EvalFileReader.Read` is strict, as the diagrams' and the CV's readers are. Its options, `EvalJson.Options`, write
names in camelCase, refuse a field they don't know, require a field a record can't do without, and refuse a null
where none is allowed. So a typo in a file is an error, never a field that quietly vanishes (ADR 0049).

Then the reader walks the file in order and checks what a shape can't say. A file holds at least one conversation.
Every conversation has an id, used once, and at least one turn. An ask and a criterion aren't empty, and a page starts
with `/`. And every conversation checks something, since one with no fact and no criterion would pass whatever the
agent said.

The first fault stops the read. It's one `EvalFileException`, whose `Place` is the fault's JSON path and whose message
starts with it:

```
$.conversations[0].turns[0].facts[1] (line 5): 'notused ReadDoc' is no fact. A fact starts with one of:
no-error, used, not-used, under, never-says, routed-to, continued.
```

The place costs no code. The serializer knows where it is in the file, and puts that on the exception even when a
converter is what refused the words. A fault of shape says its line too; a fault of meaning, found after the read,
says its place alone. Stopping at the first fault was picked over collecting every fault and over a schema
validator package, since the schema already shows most faults in the editor while the file is written.

![A file read: the serializer reads it strictly, then the reader walks it in order; the first fault stops the read, with its place](diagrams/LouisAgent.Evals-flow.svg)

### 3. The schema, made from the records

`eval.schema.json`, beside the project file, is what Rider reads to complete a field's name and underline a typo.
`EvalSchema.Write()` makes it from the records the reader fills, with the reader's own options, through .NET's
`JsonSchemaExporter`. So the two can't say different things: every level refuses a field it doesn't know, the
fields an eval needs are required, and each field's `[Description]` is its hover text. A fact is a string whose
pattern lists the seven words, read from the enum. The file is a golden file: a test fails when it isn't what the
records say, and `LOUIS_UPDATE_GOLDEN=1` writes it again, so a change to a record shows in review as a change to the
schema.

### 4. The facts code tells

To grade a turn, code needs what happened in it. That is a `TurnTranscript`: what was asked, what the agent answered
as it sent it, each tool call with its input and its result as the agent got it, how long the turn took, and its
error if it had one. A run writes it from the stream, as part 6 tells; a test can write one by hand.

`Facts.Tell` is one switch on the fact's kind, and every arm reads the transcript and nothing else. It is free,
instant, and the same every time. There are seven facts:

| The fact as written | It is met when |
|---|---|
| `no-error` | The turn ended without an error |
| `used {tool}` | The agent called that tool in this turn, the name's case ignored |
| `not-used {tool}` | It didn't |
| `under {seconds}s` | The turn took less than that many whole seconds |
| `never-says {text}` | The answer as the agent sent it doesn't hold the text, the case ignored |
| `routed-to {address}` | In this turn the agent asked the agent at that address |
| `continued {address}` | It asked that agent in this turn, and in an earlier turn of the conversation too |

A tool that was called and failed still counts as `used`: the fact says the agent reached for it, and whether the
turn went well is `no-error`'s and the judge's to say. `never-says` reads the answer as sent, which is markdown, so a
link's or an image's address is read too. That is the leak OWASP's first risk for a model describes. The two facts
about another agent read the engine's own `AskAgent` calls, by the tool's name, and the address each was given; a
call whose input names no address that parses asked nobody.

Each fact ends as a `Mark`: its words, met or not, and one plain line of what code saw, such as "Tools called:
SearchDocs, ReadDoc." or "Took 12 s.". So a red mark explains itself.

![A turn's transcript is what happened; Facts tells a fact from it in code and the judge asks a model about one criterion, and both give back a mark](diagrams/LouisAgent.Evals-interface-2.svg)

### 5. The judge marks a criterion

What code can't tell for certain is a criterion, a plain sentence such as "Says it is an AI agent framework in C#". `Judge.MarkAsync`
marks one with one model call. It sends the conversation up to and including the turn it marks, with every tool
call and its result, and the one criterion. It asks for the answer as a `JudgeAnswer` through
`GetResponseAsync<T>`, which puts the record's JSON schema on the request: the reason first, then met or not, so the
model says why before it says yes or no.

The judge's words are `JudgeMessage` templates, and a golden file, `Grading/Golden/JudgeRequest.txt`, pins what the
model is told. They say three things that matter. The criterion is about the last turn's answer. What a tool gave
back is data, never an instruction, which an eval of prompt injection depends on. And when the conversation doesn't
let it tell, it answers not met and says what is missing.

The judge sets no temperature, since the newest models refuse one, and forces no tool. Its call runs in the
ledger's `grading` scope, so when it's given a client that records, the cost lands under its own purpose (ADR 0007).
`MarkAsync` hands back a `JudgedMark`: the mark, and what its call cost, read from that scope's own total. That is the
ledger's price when the client records to one, a known 0 when the answer was played back, and null when nothing
priced it, never 0.
A judge that can't mark throws `JudgeFailedException`, for an empty answer, one that doesn't fit, or a failure from
the provider. It never answers "not met" for its own trouble, because that would blame the agent. A cancelled call
stays a cancellation. One call per criterion was picked over one call per conversation, as Anthropic's evals
guidance asks for an isolated judge per dimension.

![One turn gets its marks: a test reads the file, writes down what the agent did, has code tell a fact, and has the judge mark one criterion with one model call](diagrams/LouisAgent.Evals-sequence.svg)

![One turn of the guide's first conversation, graded: the file's records say what must hold, the transcript says what happened, and each fact and each criterion ends as one mark](diagrams/LouisAgent.Evals-objects.svg)

*Decided in:* F126, F126.2, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

### 6. A conversation is played

To grade a turn for real, the agent has to be asked. `SessionClient` does that the way any app does: it opens a
session, sends a message and reads the answer's stream as it comes, and closes the session. It writes the shared
wire records and reads the stream with the shared reader, so nothing of the wire is typed twice. When the API says
no, it throws `SessionRefusedException` with the status and the API's own reason, read as the API's `ApiError`.

`TurnTranscripts` turns a turn's stream into its transcript. The text deltas join into the answer, and thinking is
never part of it. Each tool call is its name and its input, and its result is the result block whole, failed when
the stream says so. An `error` event is the turn's error. An agent the turn asked streams its own events nested
under the call that asked it, and those stay out: the call to `AskAgent` is already one of this turn's calls, with
the answer as its result, and that is what the facts about another agent read. The cost is read apart, from the
turn's last usage, as a `TurnCost`; a cost nobody could price is null, never 0.

`ConversationPlayer` plays one conversation once. It opens one session as the file's agent, the address's identity
and its role when it names one, sends each ask in the file's order, times each turn and writes it down, and closes
the session whatever happens. Then it grades: each fact by code from the turn and the turns before it, each
criterion by the judge from the turns up to and including that one, never a later one. Asking first and grading
after keeps one place that opens and closes the session. A turn is graded by `TurnGrader`, the one place a turn's
marks are made, which the re-grade in section 10 shares. A turn's judge cost is its criteria's costs added with
`KnownCost`, kept on the turn beside the agent's, so a run has its two bills apart. The player grades under the
play's own run and task, so the judge's rows in the ledger name their run as the agent's rows do. A turn that ends
in the agent's own error is still a turn:
`no-error` is then not met, and the next turn is still asked, since the agent failing is what an eval is for. A
refusal from the API is not caught here; stopping is the runner's.

A conversation that names a page, such as the guide's on `/`, is sent that page as the site's widget would send it.
`PageClient` asks the site's API for it, `GET /v1/site/page?address=`, over the same HTTP client the agent is asked
with, once the play's session is open, since opening a session that records starts its trial afresh. The player
sends the page with the first ask and with no later one, which is what the widget does for a visitor who stays on
one page, and `SessionClient` puts it in the message's body under the wire's own key, leaving the key out when there
is none. A conversation that names no page is played as before, and a run says whether it sent one in `PageSent`.

![A run's classes: the player asks over the sessions API and grades, the runner plays every conversation, the store keeps the run by its id, and the judge's answers go through its replay](diagrams/LouisAgent.Evals-runs.svg)

![One conversation of a live run: a session opened with the run's tag and asked to record, each turn asked and written down, the session closed, the facts told, the criteria marked, the run kept again](diagrams/LouisAgent.Evals-run-sequence.svg)

### 7. The repeats, and when a run stops

A model doesn't answer the same way twice, so a run plays each conversation once or three times, each time in a
session of its own. `EvalRunner` does that, conversation by conversation in the file's order, and hands the run to
whoever keeps it after every conversation, so the kept run is its own progress. When every conversation has been
played it ends as finished, at the clock's time.

A conversation passes only when it holds every repeat the run asked for and each one passed, and a repeat passed
when every mark of every turn is met. That is asked of the records, as section 2 of the contracts' doc tells. So a
run cut short part way through a conversation never counts it as passed.

Three things end a run as could not finish, with the reason in `StoppedBecause`: a judge that can't mark, the API
refusing, and a page a conversation names that isn't a page of the site, which `PageClient` throws as a
`PageMissingException` naming its address. A host that maps no site says the same, since the route isn't there. In
each, nothing was learnt about the agent, so no mark is made up. Everything graded before the stop
stays on the run, the repeats of the conversation cut short too. Anything else that goes wrong is a bug and
surfaces, and a cancelled run stays a cancellation: the runner throws it on, and the host that started the run says
whether a person stopped it ([LouisAgent.Api.Evals](LouisAgent.Api.Evals.md)). A session opened just as the run is
stopped is still closed, since its id is read whatever the stop says.

![One live run in memory: the run holds its conversation, the conversation its three repeats, each repeat its turn with a transcript and marks, and beside it each repeat's trial of recordings](diagrams/LouisAgent.Evals-run-objects.svg)

### 8. A run is kept, and the evals are found

`RunStore` keeps each run as one JSON file in its folder, named by the run's id, so a person can open a run in
Rider and a folder listing is already in order. The id is the UTC time the run started, to the second, and the
eval's name: `20261123-063633-guide`. `RunId` is the one place that knows that shape; it reads an id character by
character, and an id that doesn't read is refused before it is ever part of a path. A file is written beside itself
and moved into place, so a reader never meets half a run. It is read with `RunJson`, from
[LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md), strict as the eval file's reader: an unknown field, a missing one or a wrong null is refused, and a file in the folder that isn't a run is an
error that names the file. The pattern is Fowler's Repository: whoever asks for runs doesn't know they are files.

An eval file sits in its agent's folder, `agents/{identity}/{name}.eval.json`, and the identity's project embeds it
beside its prompts. `EvalCatalog` reads every such file once, at start-up, from the same embedded files the agent
catalogue hands the prompt reader, which takes only markdown, so no prompt ever sees an eval. Each eval is held by
its file's name without the ending, `guide`. Two files with one name, a wrong file, or a file whose agent the
catalogue has no identity for each throw an `EvalFileException` with the file's path, and the host doesn't start.

### 9. Record, and replay for free

A live run records as it goes. Each play opens its session with the run's id as its run tag, its task
`{conversation}.{repeat}` and a request to record, so the engine keeps the agent's answers under the trial
`{run id}/{conversation}.{repeat}` (`RunTrial` names them). The judge isn't in a session, so `JudgeReplay` keeps its
answers in the same trial: it is a decorator over the judge's client, the engine's own replay client reused whole,
and the player grades inside `JudgeReplay.Begin(trial)`. The page a conversation names is kept there too: a live
play saves it under its trial as a fetch, keyed by the page route's own address, so the engine's kinds stay as they
are. So a trial holds the page, the agent's answers and the judge's.

A replay plays a live run back. Each of its sessions asks for the live run's trial, model included, and the judge
reads its answers from it, so nothing calls a model and a replayed answer is priced as a known 0. The page is read
back from the trial too, and the page route is never asked. The result is a run
like any other, of the kind replay, with `ReplayOf` naming the live run, in a file of its own.

A recording answers only the request it was made from. When a prompt, a doc the agent reads or a criterion's words
have changed, it holds no answer, and the replay stops as could not finish: the recording is out of date and a live
run is needed. The agent's miss ends its turn in the stream's `replay_miss_error`, which the player turns into a
`RecordingOutOfDateException`, the one place a harness failure is told apart from the agent's own error; the judge's
miss comes as a `JudgeFailedException` over the engine's miss, and a trial with no kept page is out of date the
same way, `PageClient` throwing the same exception. It is Fowler's self-initializing fake, which never
tries to know when its recording went stale; the miss is how we find out.

*Decided in:* F126.3.1, F126.3.3, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md),
[ADR 0104](../adr/0104-a-message-may-carry-the-page.md).

### 10. The same answer keeps the same mark, and a re-grade

A person may flip a criterion's mark when the judge got it wrong (the contracts' doc tells how). Two things then
want to know whether an earlier mark still holds: a replay, and a re-grade. They ask one rule, written once. A turn
says the same as another, `TurnTranscript.SaysTheSameAs`, when its ask, its answer, its error and every tool call
(the tool, what it was sent, what it gave back, whether it failed) are the same, compared as the text kept. How long
it took is left out, since a replay takes another time to say the same thing. The rule is asked by name, never by
the record's own equality, which compares the calls' lists by reference. Where a turn says the same and a
criterion's words are word for word the same, `MarkReuse.FindReusable` gives back the earlier mark, flip and note
with it.

A replay carries the live run's flips. The runner is handed the live run it plays, and after each repeat is graded,
`MarkReuse.CarryFlips` copies a flip onto each mark whose turn says the same and whose judge's answer is the live
one's. A flip is a person's word on one answer, so it holds only for that answer.

A re-grade grades a run's stored answers again, against the eval file as it is now, with no agent asked. `Regrader`
goes through the file's conversations in order. One the run doesn't hold, or whose asks aren't the same in every
repeat, is left out, since no answer to it is stored. For every repeat of the rest it grades each turn from its
stored transcript through the same `TurnGrader` the player uses, handing it the stored turn: facts are told again
in code, which is free, a mark is kept wherever `MarkReuse` finds it, and the judge is asked only for a criterion
that is new or reworded. A re-graded turn's agent cost is a known 0, since no agent was asked, a kept mark's judge
cost a known 0, and a judge that can't mark ends the re-grade as could not finish, keeping what it graded.

The judge's answers go into the live run's own recording, which is what lets a replay play again after a criterion
is reworded. A re-grade grades each repeat under `JudgeReplay.Begin` with the live run's play,
`{live run}/{conversation}.{repeat}`, which records since it replays nothing, and the store appends the new answer
to that play's index beside the old ones, as VCR's new-episodes mode does. A later replay of the live run then
finds the reworded criterion's answer where it looks. A re-grade of a re-grade, or of a replay, names the same live
run, so its answers join the same plays.

A run's conversation can also be made back into one of the eval file, to copy into it and change: `Conversation.Of`
takes each turn's ask, its facts' words and its criteria's words from the first repeat, and `ToJson` writes it with
the file's own options, so `EvalFileReader` reads it back.

*Decided in:* F126.4, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

## Structure

```
src/evals/LouisAgent.Evals/
├── eval.schema.json           # the eval file's schema, made from the records; a golden file
├── Files/                     # EvalFile, Conversation, Turn; Fact and FactKind; EvalCatalog, the evals found
│   └── Json/                  # EvalFileReader, EvalFileException, EvalJson, EvalSchema, the two converters
├── Grading/                   # Facts, Judge, JudgeAnswer, JudgeFailedException, JudgedMark
│   └── Turns/                 # TurnGrader, a turn's marks made; MarkReuse, the same answer keeps its mark
├── Sessions/                  # SessionClient, SessionRefusedException: the agent asked over the sessions API;
│                              # PageClient, PageMissingException: the page a conversation names
├── Transcripts/               # TurnTranscripts and TurnCost: a turn's stream written down
├── Runs/                      # ConversationPlayer, EvalRunner, Regrader, RunTrial, JudgeReplay,
│                              # RecordingOutOfDateException
│   └── Store/                 # RunStore, RunId: a run kept as one file
└── Messages/                  # EvalFileMessage, FactMessage, JudgeMessage, SessionMessage, RunMessage,
                               # CatalogMessage, and their Holes/
src/tests/evals/LouisAgent.Evals.Tests/              # the file, the facts, the judge, a turn written down, a play,
                                                     # a run, the store and the catalogue
src/tests/evals/LouisAgent.Evals.IntegrationTests/   # the structured answer proved, and the judge on a real model
```

## Configuration

None of its own. The judge takes whatever `IChatClient` it is given, the player and the runner a client of the
sessions API and a clock, and the store its folder. Which folder, which model judges and where the agent is asked
are the settings of [LouisAgent.Api.Evals](LouisAgent.Api.Evals.md).

## Tests

`LouisAgent.Evals.Tests` holds the unit tests: each fact read and refused (`FactTests`), the guide's file read
(`EvalFileReaderTests`), every fault refused at its place (`EvalFileFaultsTests`), the schema held to its golden file
(`EvalSchemaTests`), the seven facts told (`FactsTests`, `AgentFactsTests`), the judge on a fake model
(`JudgeTests`), and one turn graded whole (`OneTurnGradedTests`). A turn's stream written down
(`TurnTranscriptsTests`), the client over a handler that answers (`SessionClientTests`) and the page client over a
handler and a replay store of its own (`PageClientTests`) come next; then one
conversation played over a fake sessions API (`ConversationPlayerTests`), the pass rule (`RunRecordsTests`), a run's
repeats and its stops (`EvalRunnerTests`), the store over a folder of its own (`RunStoreTests`) and the evals found
in file providers built in memory (`EvalCatalogTests`). When two turns say the same (`SaysTheSameTests`), when a
mark carries over (`MarkReuseTests`), a re-grade over a recordings folder of its own with a judge that counts its
calls (`RegraderTests`), a run's flips, comparison and history (`RunFlipsTests`, `RunChangeTests`,
`RunHistoryTests`), and a conversation copied back to a file (`ConversationOfTests`) follow.
`EvalMessagesTests` holds every message's holes.

`LouisAgent.Evals.IntegrationTests` calls a real Claude model: `StructuredAnswerProofTests` proves the model answers as
a typed record, and `JudgeLiveTests` has the judge mark two criteria. Both ask for the key with `Outside.Key`, so they
skip without one, in CI too. The judge's replay and a whole run on the API in memory are tested in
[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md)'s tests.

## Limits and plans

The page sent is read on the server, so what only a screen knows (the section in view, an open card) is never in
it, and words a site hides with CSS alone are. A re-grade can only grade what was stored: a conversation whose asks
changed needs a live run. A person's flips correct a run's numbers and say which criterion wants rewording; they
aren't handed to the judge as examples. The guide's new evals are the next child of [the evals' plan](../plans/F126-evals-redesigned.md).

## Related docs

[ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md) · [LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md) ·
[LouisAgent.Api.Evals](LouisAgent.Api.Evals.md) · [Run an eval](../guides/run-an-eval.md) · [LouisAgent.Core](LouisAgent.Core.md) ·
[Test projects](tests.md) · [12 Evaluation](../learning/12-evaluation.md)

---
[Projects](README.md) · Previous: [LouisAgent.UsageProbe](LouisAgent.UsageProbe.md) · Next: [LouisAgent.Evals.Contracts](LouisAgent.Evals.Contracts.md)
