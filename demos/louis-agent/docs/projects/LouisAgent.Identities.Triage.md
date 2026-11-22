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
**sorter**, the **investigator** and the **reviewer** work for it; the **coder** is the coding identity on a peer.
One identity with roles (F106.3.2, Louis's pick), because the three only ever work for the manager: the manager's
voice is the mode, the three are `specialists/triage-*.md`, and the packs are names.

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

`demos/louis-agent/evals/LouisAgent.Identities.Triage.Evals`: one eval per role, folders of files the coding demo's
runner lists (`triage-sorter`, `triage-investigator`, `triage-reviewer`). Each opens its session as the role itself,
at a specialist's level (`"depth": 1` in its mode, the harness's way to reach a role, Louis's pick), with the role's
toolsets, on the order service's `order-total 1003` error for the retired code `SUMMER25`. The checks are the
harness's general ones: no error, the role's tool called, and called with the choice expected (the sorter `code`,
the investigator the area `code`, the reviewer `happy` false on a fix that charges a retired code nothing).

The first live run, on 2026-11-09 on Haiku, passed all three, for about R0.15 together; it recorded the answers, so a
run after replays them for nothing. Run it against a coding API with the triage settings and a replay folder:

```bash
AGENT_API=http://127.0.0.1:5072 dotnet LouisAgent.CodingEvalRunner.dll run triage-sorter --web record
```

## Tests

`demos/louis-agent/tests/LouisAgent.Identities.Triage.Tests`: the identity's shape, the voice as the roles catalogue
reads it, its routing naming its toolsets, and the three evals loaded as the runner loads them.

---
[Docs index](../../../../src/docs/README.md) · Related: [Projects](../../../../src/docs/projects/README.md)
