# FX257: The triage manager decides by its runbook's table

> **Plan.** How the triage team's eval gets independent trials and its manager a decision table it applies, in six
> steps. **Elsewhere:** the triage team in [its design doc](../../../demos/louis-agent/docs/projects/orchestrator-runbooks.md)
> and the evals in [LouisAgent.Evals](../projects/LouisAgent.Evals.md).

![FX257 at a glance: each trial starts clean, each runbook holds one Decisions table both agents read, and the manager applies the row and quotes it](diagrams/FX257-triage-decides-by-the-runbook-overview.svg)

## Context

Louis asked, after the triage eval's re-record for F97.1, "can we make them pass?", then "research orchestration
prompts, and see what we must change", "or redesign". The triage case `triage-another-departments-is-handed-off`
passed 3 of 3 on 2026-11-24 and failed 2 of 3 on 2026-11-26, on main and on F97.1 alike: the manager raised a
missing data file to a person, saying it "had no route to an ops manager".

Two things were found. First, the eval's cases had fixed run ids, so each run's history file grew across every run
and repeat. A fresh id per trial (built on this branch, `{trial}`) took that away, but the hand-off case still failed
2 of 3 on fresh histories, so the history was not the cause. Fresh ids also showed that `RaiseToPerson` and
`ReadTriageRun` refuse a trial's new run, where `DecideCase` takes it (Louis's pick A in F126.5.3). Second, the
manager's voice says "`hand-off` to that department's manager with the whole run", and the runbook says "**Escalate
to:** ... a missing or unreadable data file is theirs". The model reads both as a delivery it must make itself, finds
no address, and falls back to a person; step 5 of the voice, "When in doubt at any step, `RaiseToPerson`", pulls the
same way.

The research, read 2026-11-27: Anthropic's multi-agent research system ("teach the orchestrator how to delegate",
"bad tool descriptions can send agents down completely wrong paths", explicit rules because "agents struggle to judge
appropriate effort"); Anthropic's building effective agents (routing needs "distinct categories" a model can classify);
OpenAI's orchestration guide (a handoff gives ownership away, an agent called as a tool leaves it with the manager) and
its Agents SDK's handoff prompt (a model chooses better when the prompt explains handoffs). None of them gives a
decision table's wording; that is ours.

Louis's picks: "Rules in the runbooks" over prompt fixes alone or a decision in code; "Replace 'Escalate to'" over a
second section or a triage copy of the runbooks.

## Design

The policy for a case lives in one place, its method's runbook, as a **Decisions** table that replaces
*Auto-fixable* and *Escalate to*. A row says what the investigation finds, the decision, and who gets it:

| The investigation finds | Decision | Who |
|---|---|---|
| a bug in this method's own code, sure | auto-fix | the coder |
| a missing or unreadable data file, or where it is deployed | hand-off | ops |
| anything else in the code, or not sure | person | orders |

The orchestrator's voice maps *auto-fix* to `CallLouisAgentFix` and *hand-off* and *person* to `EscalateToHuman`
with the row's who. The triage manager applies the row, quotes it in its reason, and says a decision the table
doesn't cover is `person`. A hand-off is only recorded: `DecideCase` says so, and that the run goes to that
department, so no address is looked for.

![FX257's interface design: RunTrial fills an ask's {trial}; the run tools take a trial's new run as DecideCase does; the runbook's Decisions table is read by both voices](diagrams/FX257-triage-decides-by-the-runbook-interface-design.svg)

![FX257's architecture: one set of runbooks in the orchestrator's skills; the triage eval sends one with each case; the demo's API runs the triage team; the run files are new for every trial](diagrams/FX257-triage-decides-by-the-runbook-architecture.svg)

![FX257's main case, a missing data file: the investigator says hosting; the manager finds the row, quotes it and records a hand-off to ops](diagrams/FX257-triage-decides-by-the-runbook-communication-a-missing-data-file.svg)

![FX257's objects in memory for one trial: its RunTrial fills the ask; the run's file starts empty and holds this trial's steps alone](diagrams/FX257-triage-decides-by-the-runbook-objects-in-memory.svg)

### The namespace map

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `RunTrial.Id`, `.Fill`, `.TrialMark` | `src/evals/LouisAgent.Evals/Runs/` | `LouisAgent.Evals.Runs` | 1 |
| `ConversationPlayer`, `Regrader` (fill the ask) | the same | the same | 1 |
| `RunTrialTests` | `src/tests/evals/LouisAgent.Evals.Tests/Runs/` | `LouisAgent.Evals.Tests.Runs` | 1 |
| `RunTools.ReadTriageRun`, `.RaiseToPerson` (a trial's new run) | `packs/tools/LouisAgent.Packs.Triage/Tools/` | `LouisAgent.Packs.Triage.Tools` | 1 |
| The runbooks | `demos/louis-agent/hosts/LouisAgent.Orchestrator/Skills/order-service/` | content | 2 |
| `voice-orchestrator.md` | `demos/louis-agent/identity/LouisAgent.Identities.Coding/agents/louis-agent/` | content | 3 |
| The triage manager's voice, `DecideCase`'s description | `demos/louis-agent/identity/LouisAgent.Identities.Triage/agents/triage/`, the triage pack | content, tool text | 4 |
| `triage.eval.json` | the triage identity's folder | content | 5 |

### What is reused

The eval runner, the triage pack and its recorder, the orchestrator's skill loading and the runbooks themselves; no
package and no new project.

## Steps

1. **Every trial starts clean.** `{trial}` in an ask, filled with `{run id}-{repeat}` of the live run by
   `RunTrial.Fill`, sent by the player and compared by the re-grade (built: 237 eval tests). `ReadTriageRun` and
   `RaiseToPerson` take a trial's new run as `DecideCase` does; a test each, seen failing first.
2. **The Decisions table.** The 11 runbooks' *Auto-fixable* and *Escalate to* become one table each, the policy
   unchanged row for row; the runbooks doc and its picture change with them. A test holds that every runbook has the
   table and no *Escalate to*.
3. **The orchestrator reads the table.** Its voice maps each decision to its tools; its tests run.
4. **The manager applies the table.** Its voice: apply the row, quote it, no row is `person`, a hand-off is recorded
   with no address; *When in doubt* names the table first. `DecideCase` says what each decision makes happen next.
   Goldens regenerated.
5. **The eval's asks carry the new runbooks**, pasted from the files as they are now.
6. **Measured, and closed.** A triage re-record, the replay's recordings, the fix's records and its lesson, and this
   plan deleted.

## Verification

The tests of what each step touches; the full suite before the pull request. Paid: one triage live run of three
repeats (about $0.10), approved with the plan; an orchestrator run only if its tests can't hold step 3, asked first.

**Done when:** the triage eval's six conversations each pass at least 2 of 3 repeats, the hand-off case 3 of 3; its
replay test is green on the new recordings; no runbook says *Escalate to*; the docs say what the code does.

## Concepts

Step 1 is test isolation, a fresh fixture per trial (skill 12, evaluation). Steps 2 to 4 are routing with explicit
criteria in an orchestrator-workers team, the policy kept as data a person edits (skill 3, prompt design, and skill
10, multi-agent orchestration). Step 6 is measuring a change on a small eval (skill 12).

## Approved during the build

(None yet.)

---
[Plans](README.md) · [Docs index](../README.md)
