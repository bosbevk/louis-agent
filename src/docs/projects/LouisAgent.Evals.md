# LouisAgent.Evals — design

> **Kind:** library (`net10.0`) · **Path:** `src/evals/LouisAgent.Evals/` · **References:** LouisAgent.Core, LouisAgent.Evals.Contracts ·
> **Used by:** its tests today; the runs of the evals, once they are built
> **Also shows:** `component`, `object`

![The evals' library: a reader that takes one eval file into records, a schema made from those records, seven facts that code tells from a transcript, and one judge that marks a criterion](diagrams/LouisAgent.Evals.svg)

## Purpose

This project is how an agent is measured. An eval is one file of conversations a person has with the agent, and
this library reads that file and grades what happened in a turn. Some things code can tell for certain, such as
whether the agent called a tool. The rest is a plain sentence, a criterion, that a model judge marks yes or no with
its reason.

It is the first part of the evals, which are being rebuilt from scratch
([ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md)). So far it reads a file and grades a turn that a
test wrote down by hand. Nothing runs an eval against an agent yet, no host serves it, and no page shows it.

The doc follows a file from its text to its marks: how it's read, how a wrong one is refused, the schema an editor
checks it against, then the facts code tells and the criteria the judge marks.

## How it works

![The evals' library with its two test projects; it stands on the engine for the model client, the address and the ledger's scope](diagrams/LouisAgent.Evals-component.svg)

### 1. An eval file

An eval file names one agent by its address, and lists conversations. A conversation has an id, the turns a person
takes, and the page they're on when the agent is a site's guide. A turn is what the person asks, with the facts code
will check and the criteria the judge will mark. This is the guide's first conversation:

```json
{
  "$schema": "../../../../../../evals/LouisAgent.Evals/eval.schema.json",
  "agent": "louis-agent-guide",
  "conversations": [
    { "id": "guide-what-is", "page": "/",
      "turns": [
        { "ask": "What is louis-agent, in one minute?",
          "facts": ["no-error", "used ReadDoc", "under 30s"],
          "criteria": ["Says it is an AI agent framework in C#", "Names the doc it read"] } ] }
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
error if it had one. Today a test writes one by hand.

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

What code can't tell for certain is a criterion, a plain sentence such as "Names the doc it read". `Judge.MarkAsync`
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
A judge that can't mark throws `JudgeFailedException`, for an empty answer, one that doesn't fit, or a failure from
the provider. It never answers "not met" for its own trouble, because that would blame the agent. A cancelled call
stays a cancellation. One call per criterion was picked over one call per conversation, as Anthropic's evals
guidance asks for an isolated judge per dimension.

![One turn gets its marks: a test reads the file, writes down what the agent did, has code tell a fact, and has the judge mark one criterion with one model call](diagrams/LouisAgent.Evals-sequence.svg)

![One turn of the guide's first conversation, graded: the file's records say what must hold, the transcript says what happened, and each fact and each criterion ends as one mark](diagrams/LouisAgent.Evals-objects.svg)

*Decided in:* F126, F126.2, [ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md).

## Structure

```
src/evals/LouisAgent.Evals/
├── eval.schema.json           # the eval file's schema, made from the records; a golden file
├── Files/                     # EvalFile, Conversation, Turn; Fact and FactKind, the seven facts
│   └── Json/                  # EvalFileReader, EvalFileException, EvalJson, EvalSchema, the two converters
├── Grading/                   # Facts, Judge, JudgeAnswer, JudgeFailedException
└── Messages/                  # EvalFileMessage, FactMessage, JudgeMessage, and their Holes/
src/evals/LouisAgent.Evals.Contracts/                # on nothing of ours, so a page in the browser can read it
├── Grading/                   # Mark: a fact's or a criterion's mark
└── Transcripts/               # TurnTranscript and ToolCallTranscript: what happened in a turn
src/tests/evals/LouisAgent.Evals.Tests/              # the facts, the reader and its faults, the schema, the judge
src/tests/evals/LouisAgent.Evals.IntegrationTests/   # the structured answer proved, and the judge on a real model
```

## Configuration

None of its own. The judge takes whatever `IChatClient` it is given; which model a run's judge uses is the runs'
to say, once they are built.

## Tests

`LouisAgent.Evals.Tests` holds the unit tests: each fact read and refused (`FactTests`), the guide's file read
(`EvalFileReaderTests`), every fault refused at its place (`EvalFileFaultsTests`), the schema held to its golden file
(`EvalSchemaTests`), the seven facts told (`FactsTests`, `AgentFactsTests`), the judge on a fake model
(`JudgeTests`), and one turn graded whole (`OneTurnGradedTests`). `EvalMessagesTests` holds every message's holes.

`LouisAgent.Evals.IntegrationTests` calls a real Claude model: `StructuredAnswerProofTests` proves the model answers as
a typed record, and `JudgeLiveTests` has the judge mark two criteria. Both ask for the key with `Outside.Key`, so they
skip without one, in CI too.

## Limits and plans

The evals are being rebuilt, and this is their first part ([the plan](../plans/F126-evals-redesigned.md)). Nothing
yet runs an eval against an agent, keeps a run, or shows one on a page. Where an eval file sits in an identity's
project and how it's found, the pass rule over repeats, and the judge's model and its settings come with the runs.

## Related docs

[ADR 0134](../adr/0134-an-eval-is-one-file-of-conversations.md) · [LouisAgent.Core](LouisAgent.Core.md) ·
[Test projects](tests.md) · [12 Evaluation](../learning/12-evaluation.md)

---
[Projects](README.md) · Previous: [LouisAgent.UsageProbe](LouisAgent.UsageProbe.md) · Next: [Test projects](tests.md)
