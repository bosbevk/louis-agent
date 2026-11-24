# LouisAgent.Identities.Triage

> **Explanation.** Who the triage team is and why it is one identity. **Elsewhere:** the workflow in
> [the F106.3 plan](../../../../src/docs/plans/F106.3-triage-and-fix-a-service.md); the pack it ends its work with in
> [LouisAgent.Packs.Triage](../../../../packs/docs/projects/LouisAgent.Packs.Triage.md); building an identity in
> [Build an identity](../../../../src/docs/guides/build-an-identity.md).
> **Kind:** contracts library (`net10.0`, an identity's prompts)
> · **Path:** `demos/louis-agent/identity/LouisAgent.Identities.Triage/`
> **Also shows:** `activity`, `sequence`

![The triage team: the manager, and the sorter, the investigator, the reviewer and the coder who work for it](diagrams/LouisAgent.Identities.Triage.svg)

A team that triages a service's errors: the **manager** decides what each case gets and checks everyone's work; the
**sorter**, the **investigator** and the **reviewer** work for it; the **coder** is the coding identity on a peer. One
identity with roles (F106.3.2, Louis's pick), because the three only ever work for the manager: the manager's voice is
the mode, the three are its subagents, `agents/triage/subagents/{name}/voice.md`, each named by its address
(`triage.sorter`), and the packs are names. Each voice declares in its header the toolsets it always loads: the manager
`triage` and `agents`, the sorter `triage`, the investigator `triage`, `files`, `git` and `dotnet`, and the reviewer
`triage`, `files` and `git`, so a session opened as any of them has its tools whoever opens it.

## How it works

### 1. The identity

`TriageIdentity` says who the team is. Its name is `triage`, and its default voice is `triage/voice`. It loads the
packs `triage`, `files`, `git` and `dotnet`, named by its own enum. And it lists the addresses the manager may ask,
its `Calls`, from `TriageCall`: `triage.sorter`, `triage.investigator` and `triage.reviewer`.

*Decided in:* F127.3.1, [ADR 0117](../../../../src/docs/adr/0117-one-address-for-every-agent-call.md).

### 2. The manager

The manager's voice is `agents/triage/voice.md`, and it makes the manager the hub. It says how the manager
decides: the runbooks come with the case as its brief, the case is expected, for a person, for a fix or for a
hand-off, and a report is only a claim until the code has checked it. It says how the manager ends a case, with
`DecideCase` or `RaiseToPerson`. And it gives the routing, the toolsets `triage` and `agents`, so the manager asks
each role by its address with `AskAgent`.

### 3. The roles

Each role ends its work with its own typed tool. The sorter has the `triage` toolset and ends with `SortCase`, naming
the department. The investigator has `triage`, `files`, `git` and `dotnet`, is read-only by its prompt, and ends with
`ReportInvestigation`: where the error is, the cause, whose area it is and how sure it is. The reviewer has `triage`,
`files` and `git`, and ends with `ReportReview`: happy or not, and its reasons, one per line.

### 4. A run id it doesn't know

The triage pack's tools refuse a run id they don't know, so a mistyped id makes no stray run. In an eval's trial it's
different: an unknown run starts with its first step instead, since a trial's fresh session has no run behind it.

![How it works: the manager asks each role by its address, every role ends with its typed tool, and the manager ends the case](diagrams/LouisAgent.Identities.Triage-flow.svg)

![One case through the team: the sorter names the department, the investigator reports, and the manager decides](diagrams/LouisAgent.Identities.Triage-sequence.svg)

![The types: TriageIdentity, its packs and the roles it may ask, and the role files ending with the pack's tools](diagrams/LouisAgent.Identities.Triage-interface.svg)

## What proves it

The manager is measured with its team by an eval of its own, `agents/triage/triage.eval.json`, beside its voice.
Each conversation is a case on the sample order service, briefed as the feed would brief it: the run id, the
department's repository, the error's JSON line and the method's runbook. There is a case for each path the voice
names. A duplicate refund the runbook expects is ignored. A payments bug, and a method no runbook covers, go to a
person. The retired discount code in `order-total` is investigated and decided auto-fix. A data file missing on the
server is handed off to ops. And a follow-up asks the investigator again about a second retired code.

Code checks what it can. `used DecideCase` or `used RaiseToPerson` says how a case ended, and two facts prove the
routing itself: `routed-to triage.investigator` is met when the turn asked the investigator with `AskAgent`, and
`continued triage.investigator` when it asked again after an earlier turn did. The judge marks the reasons and the
briefs. No case reaches the coder: the coder is a peer, and the demo's host names none, so a live run stops at the
manager's decision and writes no code.

The demo's API host takes the evals, and the code-review app's admin shows their runs. A live baseline's first
repeats are kept in `demos/louis-agent/recordings/evals/triage/`, and `TriageEvalReplayTests` replays every case on
each build with no key, the roles' tool answers played back, so it reads nothing of the sample.

*Decided in:* F126.5.3, [ADR 0134](../../../../src/docs/adr/0134-an-eval-is-one-file-of-conversations.md).

## Tests

`demos/louis-agent/tests/LouisAgent.Identities.Triage.Tests`: the identity's shape, the voice as the roles catalogue
reads it, and its routing naming its toolsets. The eval's tests sit beside the demo's catalogue, in
`LouisAgent.Coding.Catalog.Tests/Evals/`: `TriageEvalFileTests` hold a case per path, the routing facts and no machine's
path, and `TriageEvalReplayTests` replay every case from its recordings.

---
[Docs index](../../../../src/docs/README.md) · Related: [Projects](../../../../src/docs/projects/README.md)
