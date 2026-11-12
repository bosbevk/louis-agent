# louis-agent.pack.triage

> **Explanation.** What the triage pack does and why it's built this way. **Elsewhere:** the workflow it serves in
> [the F106.3 plan](../../../src/docs/plans/F106.3-triage-and-fix-a-service.md); building a pack in
> [Build a pack](../../../src/docs/guides/build-a-pack.md); the service it reads, the louis-agent demo's
> [samples](../../../demos/louis-agent/samples/README.md); the orchestrator that read the feed before, in
> [its doc](../../../demos/louis-agent/docs/projects/louis-agent.orchestrator.md).

![The triage pack: the error feed, a run's steps, typed endings, a person and a run read back, and the departments](diagrams/louis-agent.pack.triage.svg)

An agent team that triages a service's errors needs what every department's workflow needs and no agent should hold
itself: where the errors come from, where a run's steps are kept, a typed way to say what it found or decided, a
person to raise a case to, and a way to read a run back. This pack holds those, as tools and records, so the
identities that use it stay markdown and the workflow stays a page of code (F106.3.1). The feed and the departments
are the workflow's to read, not the model's, so they are records and readers, never tools.

## What it holds

| Part | Does |
|---|---|
| `Feed/IErrorFeed`, `JsonLinesErrorFeed`, `ServiceError` | The errors not handled yet, one by its id, and the mark that one was handled, kept in a file beside the log so it survives a restart; a JSON-lines log today, a tracker's webhook later (F100). Moved from the orchestrator, which reads it from here |
| `Runs/ITriageRecorder`, `FileTriageRecorder`, `TriageRun`, `Steps/TriageStep` and its three enums | A run is its steps in order (who did what, in plain words, and why, with the time, a typed record as JSON behind a decision step, and the outcome on the last). The file recorder is one JSON-lines file per run under `TRIAGE_RUNS`, written by wire names; F106.3.3 adds one on the saved conversations' store |
| `Tools/TriageTools` | The roles' typed endings: `ReportInvestigation` (where, cause, area, confidence), `ReportReview` (happy, reasons), `DecideCase` (ignore, investigate, person, auto-fix, hand-off; why). Each value is parsed once into its enum, a bad one refused as text with the choices, a good call saved as the run's next step with its record |
| `Tools/RunTools` | `ReadTriageRun`, a run as one line per step with how it ended, for the guide, a person or the manager; `RaiseToPerson`, the case with the run so far written under `TRIAGE_PEOPLE`, and the step that says so |
| `Tools/Results/` | `Investigation`, `Review`, `CaseDecision` and their enums `DepartmentArea`, `Confidence`, `CaseDecisionKind` |
| `Departments/Department`, `DepartmentsFile` | A department as configuration: its repository, its error log, where its people read; read from `departments.yaml` with every path absolute against the file, a bad or repeated name refused. Its coder is held to its repository by `WorkspaceBoundary`, which a test proves |
| `PlainId` | The one rule for an id from outside: letters, digits, dashes and underscores, up to 64, since ids end up in branch names, commit messages and file names |
| `TriagePack`, `TriageToolset`, `TriageOptions`, `Messages/TriageToolMessage`, `prompts/toolsets/triage.md` | The pack: one toolset, `triage`, both tool classes over one recorder, off until its two settings are set, its calls one at a time since they write; the guide; the texts, pinned by a golden |

## The settings

| Setting | Means |
|---|---|
| `TRIAGE_RUNS` | The folder a run's steps are written under, one file per run |
| `TRIAGE_PEOPLE` | The folder a case raised to a person is written in, until a department's own endpoint names where its people read |

Unset, the toolset reports them missing and stays off. The departments file is the workflow's setting, not the
pack's: `demos/louis-agent/config/departments.yaml` names the code department on the sample service and an ops
department for the hand-off (F106.3.5).

## Why this way

- **A tool, not a prompt rule** (ADR 0080): asking a role to end with a typed tool call is what makes its answer a
  record the code can check; prose read by a pattern was the orchestrator's way, and a line the pattern couldn't
  read became "unknown" and was never tried again.
- **Refusal as text, never a throw** (the pack contract, ADR 0028): a bad value comes back with the choices named, so
  the model corrects itself and a call served over MCP behaves the same.
- **The run id is a parameter**: a tool reads nothing from the session, so the same tools serve a role's session, a
  person at the terminal and the guide in the site's corner.
- **Two tool classes, one toolset** (ADR 0034): the decision tools and the run tools are apart by responsibility and
  each under the size limit; the model sees one toolset.

## Its web views

`packs/web/louis-agent.pack.triage.web` (F106.3.4, ADR 0086) is the demo a site plays: `TriagePlayer` fetches a run
file, reads it with `TriageRunFile` (the recorder's own lines), and plays it a step at a time, a reading beat apart:
The player lays out two columns (F122): on the left the site's `Intro`, the stage and the controls, with the site's
`Actions` at the end of the controls and its ending under them once the case is closed; on the right `StepPopup`, which
says who took the step, what it did, why in its own words and what that kind of step is (`StepWords`), and under it
the steps so far, a list a visitor and the guide can read, scrolling inside the column. `TriageStage` draws the team
as swimlanes: a lane per agent, the one at work lit, each step played a numbered dot in its agent's lane joined in
order, a dot showing its step; where the dots go is `Runs/SwimlaneLayout`, plain code with its own tests. Back and
next stop the play; play again starts it over. The words are `TriageWords`, English by default and a site's own when given; the styles
are `Styles/pack-triage/_demo.scss`, which a site forwards. It references the framework's web, never the pack, so it
keeps its own copy of a step (`TriageStepView`), and `louis-agent.pack.triage.web.tests` writes a step with the
recorder's options and reads it back field for field. The louis-agent site plays a recorded run on `/triage-demo` until
F106.3.3's workflow runs live.

## Diagrams

![How it works: the workflow reads the feed, the roles end with typed tools, and every step is written to the run](diagrams/louis-agent.pack.triage-flow.svg)

![An investigation reported: a bad value comes back with its choices, a good one becomes a step on the run](diagrams/louis-agent.pack.triage-sequence.svg)

![The types: two tool classes over one recorder, the run and its records, and the feed the workflow reads](diagrams/louis-agent.pack.triage-interface.svg)

## Tests

`packs/tests/louis-agent.pack.triage.tests`: the feed over a small log; the recorder (steps saved out of order come
back in order with the outcome, a bad id throws, the line reads by wire names); the decision tools (a good call reads
back as the record, the refusals write nothing); the run tools (the read, the raise, the refusals); the departments
(absolute paths, a bad or repeated name refused, the demo's file reads, a department's coder cannot touch another's
repository); the pack's shape; the texts' golden. Convertibility to MCP is held by Puls's `PackConvertibilityTests`
for the packs Puls loads; this pack joins the coding demo's catalogue in F106.3.2, where the same check holds it.

---
[Docs index](../../../src/docs/README.md) · Related: [Projects](../../../src/docs/projects/README.md)
