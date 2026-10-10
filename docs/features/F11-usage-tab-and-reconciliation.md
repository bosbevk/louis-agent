# F11 · Usage tab, usage API and reconciliation with the bill

> **Status:** planned · **Milestone:** M5 · **Depends on:** F1–F3, F6, F7
>
> **Spec:** [Usage §7.1–7.2, §4.4](../specs/USAGE_AND_BUDGETS.md)
>
> **In the TODO:** tick **F11 Usage tab and reconciliation** under *Now* ([TODO](../../TODO.md)) · **Replaces:** "Track token usage / cost per session" (together with F1–F3, F6, F7)

F3 shows usage where you're working; F11 adds the overview — what was spent today and this month, where, and against
which budgets — plus a check that louis-agent's numbers match what the provider actually bills.

## User stories

**F11-S1 — Usage overview.** As an *operator*, I want a Usage tab in the web app, so that I see spending at a glance.
- *Then* it shows today and this month: cost and tokens by model, host and service; cache-hit rate; the most expensive
  sessions and tasks (each task linking to its comms-log section when it has one).
- *And* "price unknown" totals are shown separately, never added as $0.

**F11-S2 — Budgets at a glance.** As an *operator*, I want every budget's used and remaining amount and status, so that I
see what's close before it stops anything.
- *Then* each budget shows limit, used, remaining, period end and status (`ok` / `warning` / `reached`), the same data
  `GET /budgets` returns.

**F11-S3 — Usage over the API.** As a *developer*, I want usage, budgets and estimates over the API, so that other tools
can report on them.
- *Then* `GET /usage?from=&to=&group_by=…`, `GET /usage/sessions/{id}`, `GET /budgets` and `POST /estimates` behave as
  in spec §7.1, behind `AGENT_API_KEY` like the other endpoints.

**F11-S4 — Match the bill.** As an *operator* with an organisation account, I want louis-agent's ledger compared with the
provider's own usage and cost reports, so that I trust the numbers.
- *Given* `ADMIN_API_KEY` *then* once a day the provider's usage and cost reports for the previous day are read (report
  reads, no tokens) and compared with the ledger by model; differences over 5% are flagged in the Usage tab and the log
  with likely causes (missing price, wrong prefix, shared key).
- *Given* no admin key (e.g. an individual account) *then* the tab says reconciliation is unavailable and why.

## Design

- **`UsageQuery`** (core): aggregates the ledger (via `UsageHistory`, F6) by period and grouping; used by the endpoints.
- **Endpoints** in a new `UsageEndpoints.cs` in `louis-agent.api`, following `HistoryEndpoints`' style; `POST /estimates`
  calls `Estimator` (F6).
- **Web:** `UsagePanel` (sidebar tab, like Branches) and `UsageViewer` (main area), a `UsageState` service with
  `Changed` events like `GitHistory`; charts kept simple (tables and bars) and readable in both themes.
- **Reconciliation:** `ReconciliationService` (hosted background service in the API, daily) calling the provider's
  organisation usage and cost report endpoints with `ADMIN_API_KEY` over HTTP; results appended to
  `logs/reconciliation.jsonl`; shown in the Usage tab. Recommend a dedicated API key or workspace for louis-agent so the
  reports aren't blended with other software.

## Implementation steps

1. **`UsageQuery`.** *Test:* grouping and periods on a fixture ledger; unpriced totals separate.
2. **Endpoints.** *Test:* responses match fixtures; key required.
3. **Usage tab** (panel + viewer + state). *Test (manual):* figures match `GET /usage`; both themes readable.
4. **Budgets section.** *Test (manual):* matches `GET /budgets`; amber and red states.
5. **Reconciliation service.** *Test:* with a stubbed HTTP handler, differences computed per model; > 5% flagged; no key
   → disabled message.
6. **Docs:** README (web app, API table), AGENT_COMMUNICATION (endpoints), SETUP (`ADMIN_API_KEY`), spec status.

## Done when

- Stories' criteria pass; tests added; suites pass.
- Where an admin key exists: a benchmark day's ledger is within 5% of the provider's report (spec §9).
- **Then finish up** (see [Finishing a feature](README.md#finishing-a-feature)): tick **F11** in the TODO,
  set *Status* to done here and in the features table, and note it in the spec's implementation map.

---
[Features](README.md) · Previous: [F10 Route settings](F10-route-settings.md) · Next: [F12 Response speed](F12-response-speed.md)
