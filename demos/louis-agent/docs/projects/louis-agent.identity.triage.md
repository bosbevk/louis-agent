# louis-agent.identity.triage

> **Explanation.** Who the triage team is and why it is one identity. **Elsewhere:** the workflow in
> [the F106.3 plan](../../../src/docs/plans/F106.3-triage-and-fix-a-service.md); the pack it ends its work with in
> [louis-agent.pack.triage](../../../packs/docs/projects/louis-agent.pack.triage.md); building an identity in
> [Build an identity](../../../src/docs/guides/build-an-identity.md).

A team that triages a service's errors: the **manager** decides what each case gets and checks everyone's work; the
**sorter**, the **investigator** and the **reviewer** work for it; the **coder** is the coding identity on a peer.
One identity with roles (F106.3.2, Louis's pick), because the three only ever work for the manager: the manager's
voice is the mode, the three are `specialists/triage-*.md`, and the packs are names.

## How it works

- `TriageIdentity`: the name `triage`, the voice `triage/voice` as the default, the packs `triage`, `files`, `git`
  and `dotnet` by its own enum.
- `Prompts/agent/triage/voice.md`: the manager as the hub, how it decides (the runbooks come with the case as its
  brief; expected, a person, a fix or a hand-off; a report is a claim until the code checked it), how it ends
  (`DecideCase`, `RaiseToPerson`), and its routing (`triage`, `specialists`).
- The roles, each ending with its typed tool: the **sorter** (`triage`) with `SortCase` and the department; the
  **investigator** (`triage`, `files`, `git`, `dotnet`, read-only by its prompt) with `ReportInvestigation`, where,
  the cause, whose area and how sure; the **reviewer** (`triage`, `files`, `git`) with `ReportReview`, happy or not
  and its reasons, one per line.
- The triage pack's tools refuse a run id they don't know, so a mistyped id makes no stray run; in an eval's trial
  an unknown run starts with its first step instead, since a trial's fresh session has none behind it (Louis's pick).

## What proves it

`demos/louis-agent/evals/louis-agent.identity.triage.evals`: one eval per role, folders of files the coding demo's
runner lists (`triage-sorter`, `triage-investigator`, `triage-reviewer`). Each opens its session as the role itself,
at a specialist's level (`"depth": 1` in its mode, the harness's way to reach a role, Louis's pick), with the role's
toolsets, on the order service's `order-total 1003` error for the retired code `SUMMER25`. The checks are the
harness's general ones: no error, the role's tool called, and called with the choice expected (the sorter `code`,
the investigator the area `code`, the reviewer `happy` false on a fix that charges a retired code nothing).

The first live run, on 2026-11-09 on Haiku, passed all three, for about R0.15 together; it recorded the answers, so a
run after replays them for nothing. Run it against a coding API with the triage settings and a replay folder:

```bash
AGENT_API=http://127.0.0.1:5072 dotnet louis-agent.coding-eval-runner.dll run triage-sorter --web record
```

## Tests

`demos/louis-agent/tests/louis-agent.identity.triage.tests`: the identity's shape, the voice as the roles catalogue
reads it, its routing naming its toolsets, and the three evals loaded as the runner loads them.

---
[Docs index](../../../src/docs/README.md) · Related: [Projects](../../../src/docs/projects/README.md)
