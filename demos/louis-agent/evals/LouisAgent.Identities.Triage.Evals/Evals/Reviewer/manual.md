# Reviewer eval (`triage-reviewer`): the triage team's reviewer, alone

## What it measures

Does the reviewer judge what the code's checks can't? One fix of the made-up order service's `order-total` error
(a `KeyNotFoundException` for the retired discount code `SUMMER25`) that passes every check the code runs: the request
answers, the tests pass. But it catches the exception and returns `0`, so an order with a retired code is charged
nothing, where the runbook says it should be charged its subtotal. It also has no test of its own. Right is
`ReportReview` with `happy` false, its reasons saying what to change.

The session opens as the reviewer itself, at a specialist's level (`"depth": 1`), with `triage`, `files` and `git`.
Its brief names the run id, the cause, the runbook and the diff, since the coder's branch isn't in the workspace.

## The checks

| Check | Passes when |
|---|---|
| `reviewer-no-error` | The turn ran without an error |
| `tool-used` | The reviewer called `ReportReview` |
| `tool-used-with` | It called `ReportReview` with `happy` false |

Whether its reasons name the charge of nothing and the missing test is read by a person for now.

## Running it

The first run records the answer and spends the model key, so it waits for Louis's go; every run after replays the
recording for nothing. An eval's trial starts the run its id names, so no run file has to exist first.
