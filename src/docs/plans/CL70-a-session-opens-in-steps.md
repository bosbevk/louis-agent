# CL70 A session opens in steps

A plan, for Louis's approval: the API's `AgentSessions.Create` becomes a method object. Its eleven parameters become
one record, `SessionRequest`; its body moves into `SessionOpening`, whose decisions are its fields and whose jobs are
small named steps; and the nine messages it builds from anonymous objects get their holes records. No behaviour
changes. Four steps, one commit each, on `refactor/CL70-a-session-opens-in-steps`.

![CL70 in four steps: the holes records, the request as one record, the opening as a method object, then the docs](diagrams/CL70-a-session-opens-in-steps-overview.svg)

## Context

Louis, 2026-11-22, of `AgentSessions.Create` on his screen: "why is this so ugly can we not add a pattern here like a
builder? or something better". Three patterns were read at Refactoring.Guru that day and put to him. He picked **A, a
method object with its request as one record** ("Transform the method into a separate class so that the local
variables become fields of the class"), over a builder (it would tidy only the calls, the checks still in one long
`Build()`) and a chain of checks (more types for a list of rules that never changes at run time). He placed it after
CL68.3 and before CL69.

What was read for this plan, on `main` at `4331a1947`. `Create` (`src/hosts/LouisAgent.Api/Sessions/AgentSessions.cs`,
229 lines) takes `toolsets, tags, depth, role, place, llm, tool, format, replay, opener, identity` and in about 45 lines
finds the identity, checks the depth, works out the level, checks a lookup agent's tool, picks the toolsets and adds the
role's own, refuses a lookup agent that loads `agents`, finds the chain of asks, builds the engine and registers the
session. Its private helpers (`LevelOf`, `ChainOf`, `Unchosen`, `Who`, `FindIdentity`, `CheckLookup`) serve only it. It
builds nine `ApiErrorMessage` texts from anonymous objects (ADR 0119 forbids them): `SessionDepthInvalid`,
`ToolOnlyForLookups`, `NoToolsetForTool`, `LookupCannotAsk`, `LookupNeedsTool`, `LookupNeedsParent`,
`LookupParentUnknown`, `ToolNeverHandedDown`, `ParentLacksTool`. It has 65 callers: `SessionHandlers` in the API,
and 64 in the API's tests (49 in `AgentSessionsTests`). The API's holes records sit in `Messages/Holes/{area}/`, and
`Sessions/` already holds six source files, the most a folder may.

## Design

### What changes

| Today | After |
|---|---|
| `Create(toolsets, tags, depth, role, place, llm, tool, format, replay, opener, identity)` | `Create(SessionRequest request)` |
| About 45 lines of checks and choices in one method, six private helpers beside it | `SessionOpening`: the request and the decisions as fields, one small method per job |
| Nine messages from anonymous objects | Nine holes records in `Messages/Holes/Opening/`, each made as a named local first |

### The shape

`SessionRequest` is a record (Fowler's *Introduce Parameter Object*) with the eleven values, each optional with today's
default, so a caller names only what it sets: `sessions.Create(new SessionRequest(Toolsets: [web], Depth: 1, Role:
TestAgent.Specialist))`. `SessionOpening` (Refactoring.Guru's *Replace Method with Method Object*) takes the engines and
the request, and its `Open()` reads as the order the checks run in: `Identity()`, `CheckDepth()`, `Level()`,
`CheckTool()`, `Toolsets()`, `CheckLookupAsksNoOne()`, `Chain()`, `Engine()`, each a few lines, each throwing as
`Create` throws today. `AgentSessions.Create` becomes: open it, register the session, return it.

![The interface design: SessionRequest, SessionOpening and its steps, AgentSessions.Create on top of them, the holes records](diagrams/CL70-a-session-opens-in-steps-interface-design.svg)

![The architecture: SessionHandlers reads the wire request into a SessionRequest; AgentSessions opens it through SessionOpening and the engine factory](diagrams/CL70-a-session-opens-in-steps-architecture.svg)

![POST /v1/sessions: the handler builds the request, SessionOpening runs its steps in order, the engine is got or made, the session is registered](diagrams/CL70-a-session-opens-in-steps-communication-a-session-opens.svg)

![Objects in memory: one SessionOpening for one request, holding the identity, the level, the toolsets and the chain it decided, then the session it made](diagrams/CL70-a-session-opens-in-steps-objects-in-memory.svg)

### Namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| Nine holes records (`SessionDepthInvalidHoles` and the rest) | `src/hosts/LouisAgent.Api/Messages/Holes/Opening/` | `LouisAgent.Api.Messages.Holes.Opening` | 1 |
| `SessionRequest` | `src/hosts/LouisAgent.Api/Sessions/Opening/` | `LouisAgent.Api.Sessions.Opening` | 2 |
| `SessionOpening` | `src/hosts/LouisAgent.Api/Sessions/Opening/` | `LouisAgent.Api.Sessions.Opening` | 3 |
| `AgentSessions.Create`, `SessionHandlers`, the 64 test calls | `src/hosts/LouisAgent.Api/`, `src/tests/hosts/LouisAgent.Api.Tests/` | — | 2, 3 |

### What is reused

The holes pattern the API already has (`Messages/Holes/Sessions/`), `MessageHoles.Problems` for the check, and every
existing session test as the safety net: no behaviour changes, so the 383 API tests must pass unchanged in what they
assert. No package is added.

## Steps

One commit each, with a review stop after each.

1. **The nine holes records.** Each `ApiErrorMessage` member `Create` uses carries `[Holes]`, and each call makes its
   record as a named local. Test first: the API's holes test covers `ApiErrorMessage` too, and fails on the nine
   members without a record.
2. **The request as one record.** `SessionRequest`, and `Create(SessionRequest)`; `SessionHandlers` and the 64 test
   calls pass one. Test first: `SessionRequestTests` holds that a request with no values is today's default session
   (no toolsets named, depth 0, no role); it fails to compile until the record exists, then on its assertion.
3. **The opening in steps.** `SessionOpening` with its named steps, the six helpers moved into it, `Create` a few
   lines. A pure move of behaviour: the test is every session test passing unchanged, and a file-size check that
   `AgentSessions.cs` is now well under its limit.
4. **The docs, and closed.** The API's design doc on how a session opens, its picture, the write-up's section in
   `20-extensibility.md` (skill 20), and this plan folded (`/finish-a-feature`).

## Verification

- **The tests** of each step, and before the pull request, with `main` merged in, the full unit suite.
- **Free checks by hand:** a chat on the site on :5080 opens and answers, and the guide asks a showroom business.
- **No paid run:** nothing the model reads changes.
- **Done when:** `Create` takes one `SessionRequest` and is a few lines, `SessionOpening` holds the steps, no message in
  `AgentSessions` or `SessionOpening` is built from an anonymous object, and the full unit suite is green.

## Concepts

- **Refactoring without changing behaviour** (steps 2 and 3): the tests stay as they are and must keep passing, which
  is what lets a large method move safely. GOAL.md skill 18, *Testing and shipping*.
- **A method object** (step 3): when a method's locals are too tangled to extract, they become fields and the steps
  become methods. Skill 20, *Extensibility*.

## Approved during the build

Nothing yet.

## The records

In the commit that saves this plan: its row in the plans' index, and CL70 named in the TODO.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [ADR 0119](../adr/0119-declare-name-then-pass.md) · [ADR 0051](../adr/0051-flat-conditionals-and-named-endpoints.md)
