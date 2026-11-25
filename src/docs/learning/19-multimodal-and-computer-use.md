# 19 · Multimodal and computer use: a photo or a PDF reaches a tool

> **Skill:** #19 in the [goal](../GOAL.md)'s map, with #20 (extensibility) for how a tool gets its model and #13
> (cost) for its ledger row · **Stage:** F171.1 *A photo or a PDF reaches a tool*, the bill checker's first child and
> the framework's part of it (2026-11-24) · **Written by** Claude, for Louis to read · **Next:** F171.2, the bills
> pack, the first real tool on this path

What this answers: how a photo of a municipal account, or the PDF the municipality mailed, gets from a person's phone
to a tool that reads it, without the photo ever entering the conversation, and without a pack reaching into the
engine for a model.

![Multimodal: a chat keeps a photo or a PDF, the model sees only its name, a pack's tool reads it with the model it was handed, and a replay finds it again by its bytes](diagrams/19-multimodal-and-computer-use.svg)

## Before and after

| | Before F171.1 | After F171.1 |
|---|---|---|
| What a message carries | Text, and text attachments | That, or a photo or a PDF as its bytes and media type |
| What the API keeps | Text attachments, by name | Those, and photos and PDFs, checked: five kinds, 10 MB each, 20 MB a chat |
| What the conversation's model sees of a file | Its text, in a fenced block | For a photo or a PDF, one line: its name, kind and size |
| How a pack's tool gets a model | It can't, short of building its own client | The program's catalogue hands it one, wrapped once |
| What a tool's model call costs, in the ledger | Nothing recorded | A row whose purpose is `tool`, inside the turn |
| A tool's call in an eval's replay | Not replayed | Replayed by its exact bytes and the record it asks for |

## Prove the adapter before building on it

The whole feature stands on one claim about someone else's code: that Microsoft.Extensions.AI's adapter for Claude
sends a `DataContent` holding a JPEG or a PDF the way Claude wants it, and brings a typed record back through
`GetResponseAsync<T>`. So the second step built nothing. It sent a made-up account, drawn as an HTML page and printed
by headless Chrome to a PDF and a screenshot, through the adapter exactly as a host builds it, and asked for the
account number and the total. The plan said it would stop right there if either reading failed.

A third case reads a real bill. Louis had one in his downloads, and a real bill is busier than anything drawn for a
test. The repository is public, so the bill never goes in it: the test reads the file named by `BILL_PDF` on the
machine that runs it, skips without it, and checks only that a number and a total come back, writing nothing of the
bill to its output.

**What was measured: nothing yet.** These readings and the whole path's live test in step 10 need a key, and they
had not been run when this was written. So whether Haiku 5.5 reads the made-up account, and what a reading costs,
is not known from this work. That is the first thing to run.

## The bytes go to the tool, the name goes to the model

A photo is big, and a conversation is sent again with every turn. If the bytes went into the conversation, every
later message of the chat would carry the photo again, paid for each time. So they don't. The API keeps the file in
memory with the session and writes one line into the turn's prompt in its place: the file's name, its kind and its
size. The conversation's model reads that line and hands the name to the tool, and only the tool's own request
carries the bytes, once. This is context engineering in its plainest form: refer to a big thing by name, and fetch
it only where it is used.

The bytes travel inside the message's JSON as base64, the way the admin's price import already sends a file, so the
API needed no upload endpoint and no form. Base64 makes a 10 MB file about 13.4 MB, and Kestrel's default limit is
30,000,000 bytes, so it fits; Azure Container Apps' front door publishes no body limit that could be found, so that
is still to check when this goes live. The checks come before the turn starts, so a refused file starts nothing,
and the refusal says which check failed.

## A model handed in, not reached for

The first plan had the engine read files: an `IDocumentReader` in the pack context and a reader in core. It was
rewritten before the build. A tool that needs a model takes Microsoft's `IChatClient` in its constructor, as ADR 0028
now says, so the same tool works outside this framework with whatever model someone hands it. The engine's part
shrank to two things: keeping the files, and wrapping the models.

Wrapping is the part a pack can't do for itself. Every model request must be measured (ADR 0007), replayed for free
in an eval, and traced, and only the engine holds the ledger, the recordings and the tracer. So the host wraps the
instance's model and each profile's once, and hands them to the program's catalogue as a function's argument,
`AgentHost.Build(models => …)`. That function is the composition root, the one place at start-up where a program
wires its parts together, and it picks each pack's model in code. A wrong profile name stops the host at start-up,
not at a user's first upload.

A tool's call is a one-off request with its own prompt, so the engine treats it as it treats a summary (Louis's pick,
"Like a summary"). It isn't marked for the prompt cache, where a write costs 25% more and would rarely be read back.
It isn't counted in a sub-session's prompt budget, which measures the conversation's own prompt as it grows; counted,
one reading could have cut a turn's tools early. And the cache-gaps report leaves it out. Louis's question on the way
was the right one: why is any of this in core, and not just a pack? Because the three things a model call must go
through are the engine's, and a pack is not allowed to reach them.

## Replayed by its bytes

An eval replays a recorded run without paying for it, so a reading must be found again by exactly what it asked. The
recording's key already hashed each message's whole contents and the request's options, which include the file's
bytes and the record's schema, so the test for it passed as written: the same file answers from the recording, and a
file one byte different, or another record asked for, is a miss that never calls the model.

## What didn't work, or took a second try

**A rule the first draft missed.** The real-bill case first skipped with a raw `Assert.Ignore`, which a rule test
refuses: a test asks for anything outside it through `Outside`, so CI can't go green by quietly running nothing. A
file kept on one machine was none of `Outside`'s three kinds, so it gained a fourth, `LocalFile`. The miss came from
not running the rule tests after adding the case.

**A process-wide set of prompts.** The engine reads its prompts from one static source, and refuses a new pack's
prompts once any prompt has been read, so a test that builds a host with a new pack mid-run fails. Core's tests
register every prompt once, before anything is read, and a made-up pack must do the same; its guide also has to
belong to the shared test catalogue, so the pack in core's tests has no guide at all.

**A handler at its size limit.** The API's message handler held 172 lines of code against the limit of 200, so the
file checks couldn't go there as the plan said. They went on the type that keeps the files, which asks one question
and holds its limits, and the handler only asks it.

## Related

- [LouisAgent.Core](../projects/LouisAgent.Core.md#15-a-photo-or-a-pdf-a-tool-reads), part 15, and
  [LouisAgent.Api](../projects/LouisAgent.Api.md), what a session keeps
- [Build a pack](../guides/build-a-pack.md#when-a-tool-needs-a-model-or-reads-a-photo-or-a-pdf), the tool's shape
- [ADR 0028](../adr/0028-packs-stay-convertible-to-mcp.md), a tool that needs a model takes `IChatClient`

---
[Docs index](../README.md) · Related: [Learning write-ups](README.md) · [The goal](../GOAL.md)
