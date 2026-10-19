# F17 · Orchestrator agent: a routing workflow, with an agent for the exceptions

> **Status:** merged into F39 *Orchestration*, Part B (2026-10-19): [the platform plan](../plans/platform.md)'s
> Phase 6 rebuilds the orchestrator on the identity split, as the `voorman` identity; the local-triage items
> (`EscalateToHuman` idempotent per error, contradicting decisions refused) are its B5's tests; this doc stays as its
> design · **Milestone:** M4 (after F10; before build mode) · **Depends on:** F4 (the caching strategy),
> F8 (S4, results as tool calls), F10 (route settings), F18 (its eval)
>
> **Spec:** [Orchestrator design](../specs/ORCHESTRATOR_DESIGN.md) · **In the TODO:** merged, so its line is gone;
> its work is on **F39 Orchestration** under *Next* ([TODO](../../TODO.md))

The orchestrator POC triages each error with a model that, for an auto-fixable error, always calls louis-agent and
then the same three checks in the same order. It already follows much of Anthropic's guidance: a full brief for its
sub-agent, the error fenced as untrusted data, verification from ground truth (git, a replay, the tests) rather than
louis-agent's claim, a condensed result back, a small tool set, and a person merges. F17 rebuilds it on the rest of that
guidance ([building effective agents](https://www.anthropic.com/engineering/building-effective-agents)):

- **Simplest thing that works:** a workflow where the steps are known, an agent only where the next step depends on
  what was found. F17 uses both, each where it fits, and measures the result against today's all-agent orchestrator.
- **Consolidate tools that are always used together**
  ([writing tools for agents](https://www.anthropic.com/engineering/writing-tools-for-agents)).
- **Route by difficulty:** classification is cheap work for a cheap model at low effort.
- **Evaluate early with a small real sample** ([multi-agent research
  system](https://www.anthropic.com/engineering/multi-agent-research-system)).

## Workflow or agent

| Case | Steps known? | Handled by | Why |
|---|---|---|---|
| A 404 the runbook lists as expected | Yes | Workflow | A lookup; a model adds cost and a chance to be wrong |
| An auto-fixable error, fix confirmed first time | Yes: fix → confirm | Workflow | Same order every time; code runs it cheaper and the same way |
| A data problem the runbook says to escalate | Mostly | Workflow; the model writes the escalation | The route is fixed; only the explanation needs a model |
| A fix that fails confirmation | No | Agent | Retry with what failed, escalate, or spot a different bug |
| An error that matches no runbook rule cleanly | No | Agent | Needs judgement before any action |
| Several errors from one root cause | No | Agent | Seeing they belong together and asking for one fix is reasoning a fixed flow can't do |

## User stories

**F17-S1 — Classify, then route.** As an *operator*, I want each error classified by a model and the known paths run as
code, so that routine errors cost less and are handled the same way every time.
- *Given* an error event *then* one classification request (no tools except `RecordDecision` from F8-S4) returns
  `expected`, `fixable`, `escalate` or `unclear`, with the runbook rule it matched.
- *And* `expected` → acknowledge, `escalate` → escalate with the runbook's team, `fixable` → fix then confirm, all in
  code; the comms log shows which path ran.

**F17-S2 — The agent takes over the exceptions.** As an *operator*, I want the agent to handle what the workflow can't,
so that judgement is spent only where it's needed.
- *Given* `unclear`, or a fix that fails confirmation *then* the triage agent (today's engine, with its tools) gets the
  event and what the workflow found, and decides; its decision is recorded with `RecordDecision`.
- *And* related errors in one batch (same method, same exception, one run) are offered to the agent together, which may
  ask for one fix for all of them.

**F17-S3 — One confirm tool.** As a *developer*, I want commit check, replay and tests in one call, so that confirming a
fix is one step for the workflow and one tool call for the agent.
- *Given* `ConfirmFix(errorId, commit, branch)` *then* it runs all three and returns each result and `CONFIRMED` or
  `NOT CONFIRMED`; the three separate tools are removed.

**F17-S4 — Its own settings.** As an *operator*, I want the orchestrator's model, thinking, rounds and cache policy set
apart from louis-agent's, so that each agent is tuned to its own work.
- *Given* the orchestrator's route settings (F10) *then* classification and the agent each use their own model and
  thinking (default: Haiku 5.5, low).
- *And* its prompt prefix (`orchestrator.md` + runbooks) is cached with the provider's caching strategy (F4); the
  lifetime (5 minutes or 1 hour) is chosen from the measured gaps between triages, and the demo drops F4's
  `LLM_CACHE=off` override.
- *And* that lifetime is the orchestrator's own, per marker as in F4 ([diagrams 4 and 5](F04-prompt-caching.md#how-caching-works)):
  its prefix's lifetime comes from `usage-probe -- --cache-gaps` for the `orchestrator` host (its gaps are the waits
  for louis-agent's fixes: about a minute in the demo, so 5 minutes; 10–30 minutes in production, so 1 hour), and its
  tail stays 5 minutes. louis-agent's lifetime is set separately, from its own gaps.

**F17-S5 — Measured against today.** As a *developer*, I want the new orchestrator compared with today's on the same
cases, so that the change is kept only when decisions hold and cost falls.
- *Given* F18's `triage` suite (the demo's 13 events plus cases for every row of the table above: a failing fix, no
  matching rule, three errors with one cause; louis-agent and the checks replayed from recordings, so no fix runs)
  *then* today's orchestrator is the baseline.
- *Then* the new one decides at least as many cases right, at a lower cost per error; and the full demo (**needs
  Louis's go-ahead**) still confirms 10/10 fixes.

## Design

- **Classification** is a single request with the runbooks as the system prompt and `RecordDecision` as its only tool;
  thinking low. Its result drives a `switch` in code (`TriageWorkflow`), not more model turns.
- **The agent** is today's `AgentEngine` with `ServiceTools` (now with `ConfirmFix`), entered only for `unclear` or a
  failed confirmation, with the workflow's findings in its first message.
- **Sequential fixes, on purpose:** Anthropic finds parallel sub-agents suit wide, independent work and fit poorly on
  tightly coupled work such as most coding. Fixes to one repository stay one at a time; build mode inherits this.
- **Out of scope** (stay in the orchestrator design and the backlog): pull requests instead of branches, a webhook
  trigger, a real error tracker, production topology, and the implementation-tier question (core vs Tool Runner).

## Implementation steps

1. **`ConfirmFix`.** *Test:* each check's pass and fail paths against a throwaway repository (as today's tests do).
2. **Classification request with `RecordDecision`.** *Test (fake client):* the decision and rule are parsed; a reply
   without the tool call falls back to the `DECISION` line (F8-S4).
3. **`TriageWorkflow`:** expected / escalate / fixable paths in code. *Test:* each path calls the right tools in order;
   the comms log names the path.
4. **Hand-over to the agent** for `unclear` and failed confirmation. *Test:* the agent's first message carries the
   workflow's findings; its decision is recorded.
5. **Grouped errors** in one batch offered together. *Test:* three related events reach the agent as one hand-over.
6. **Own settings and cache policy** on F10's route settings. *Test:* classification requests carry the orchestrator's
   model and low thinking; the prefix is marked for caching.
7. **Triage suite** (F18): cases, recorded replies, the baseline on today's orchestrator (**go-ahead**, cents).
8. **Measure:** the new orchestrator on the suite, then the full demo (**go-ahead**). Record decisions right, cost per
   error, cache-hit rate and fixes confirmed.
9. **Docs:** the orchestrator project doc, `orchestrator.md`, the runbooks (`projects/orchestrator-runbooks.md`), the orchestrator design spec, the goal map.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Triage suite: decisions right ≥ baseline, cost per error lower; demo 10/10 confirmed; numbers recorded here.
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): move **F17** to the CHANGELOG, set
  *Status* to done here and in the features table, update skill 10 in the goal map and its write-up.

---
[Features](README.md) · Previous: [F16 Summariser](F16-summariser.md) · Next: [F18 Evaluation](F18-evaluation.md)
