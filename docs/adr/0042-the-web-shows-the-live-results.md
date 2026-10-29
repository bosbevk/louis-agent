# 0042. The web shows the live results: after a write, the page fetches again

**Status:** Accepted · **Date:** 2026-10-26 (F18 step 6a.4, Louis on the Judge-against-people card: "investigate these
values", then "the web must always reflect the live results", "this should be a rule outward") · **Recorded:**
2026-10-26

## Context

The run page loads a run once, when it opens (`GET /v1/admin/benchmarks/runs/{id}`), and hands that JSON to its
panels. The review panel of step 6a.3 writes a grade to the API (`PUT …/reviews/…`) and tells the page's state that a
grade was given, so the judge's chip shows again, but nothing fetched the run again. The Judge-against-people card kept
counting from the copy the page had loaded: it showed 1 passed and 1 failed while the store held 3 and 1. Louis knew
every grade had agreed with the judge and saw numbers that said otherwise.

The same shape exists elsewhere: a re-score starts a new run and the list learns of it on the next poll; the chat
page keeps its own copy of a turn until the stream ends. A page that holds a copy and edits the copy drifts from the
store as soon as anything else writes, and the person reading it can't tell.

These were considered:
- **Count locally in the component.** The card adds the grade it just saw to its own counts. Instant and no request,
  but the rule for which grade counts (the latest per reviewer; an unsure takes a pass back) would exist in the API and
  again in the browser, and another reviewer's grades never arrive.
- **A reload button.** Least code, but it asks the person to know the numbers are stale, which is the thing that went
  wrong.
- **Fetch again after every write.** One request per write, the same read the page opened with, so every panel shows
  what the store holds and no logic is repeated. Chosen.

## Decision

- **What the portal shows is what the store holds now.** A page or component renders what it last fetched from the
  API, and after any write it makes (a grade, a re-score, a name kept, a setting changed) it fetches again what it
  shows. It never adds its own write to a copy, never keeps a count the API also keeps, and never asks the person to
  reload.
- **Change flows through a shared state's event.** A write that other components show goes through a scoped state
  (`ReviewerState`, `CurrencyState`) that raises `Changed`; a page or component that shows the result subscribes, fetches
  again, and unsubscribes when disposed. A component never reaches into its page (W2); the event is how the page learns.
- **A re-fetch keeps the person's place.** The open conversation, the chosen repeat and a typed note survive the fetch:
  the page replaces its data, not its state.
- **The API computes, the web shows** ([0007](0007-measure-every-request.md)): a count or a share the API serves is
  never computed again in the browser from the rows.

## Consequences

- **Review holds the rule.** A component that updates its own copy after a PUT, or counts rows the API already counts,
  is sent back; `AgreementView` reads the API's `agreement` and never the `human_grades` rows.
- **One request per write.** A grade costs a second GET of the run (a few hundred KB on a big run). Acceptable: grading
  is a person's pace, not a loop's. If a page grows too heavy, the API gets a smaller read for the part that changed,
  not the browser a local count.
- **Step 6a.4 adds the first case:** the run page subscribes to `ReviewerState.Changed` and fetches the run again
  after a grade, keeping the open task; the chips, the panel and the Judge-against-people card then agree with the
  store.
- **Existing pages aren't swept.** The runs list's poll and the chat page's stream stay as they are; a page that keeps
  a copy is changed when a step touches it.

## Related

- [0007](0007-measure-every-request.md): every host shows a turn's usage from the same records, so the views and the
  ledger agree; this is the same rule for every stored result.
- [0010](0010-folders-by-concern.md): state flows down as parameters and up as events (W2).
- [F18 Evaluation](https://github.com/bosbevk/louis-agent/blob/pre-cl24/docs/features/F18-evaluation.md): step 6a.4 (the stale agreement card).
