# Investigator eval (`triage-investigator`): the triage team's investigator, alone

## What it measures

Does the investigator read the code behind an error and put the cause in the right area? One error of the made-up
order service (`demos/louis-agent/samples/order-service`): `order-total 1003` throws a `KeyNotFoundException` for the
retired discount code `SUMMER25`. The runbook says a retired code should give no discount, so the lookup that throws
is the cause, and a change in this repository fixes it: right is `ReportInvestigation` with the area `code`.

The session opens as the investigator itself, at a specialist's level (`"depth": 1`), with `triage`, `files`, `git`
and `dotnet`, read-only by its prompt. Its brief names the run id, the repository, the error and the method's
runbook, as the manager's would.

## The checks

| Check | Passes when |
|---|---|
| `investigator-no-error` | The turn ran without an error |
| `tool-used` | The investigator called `ReportInvestigation` |
| `tool-used-with` | It called `ReportInvestigation` with the area `code` |

Where it points and how sure it is are read by a person for now; a check on `where` naming `OrderTotal.cs` can join
when a run has shown how the investigator words it.

## Running it

The first run records the answer and spends the model key, so it waits for Louis's go; every run after replays the
recording for nothing. The runner's workspace must hold the sample at the path the brief names (the repository, as
the coding demo mounts it). An eval's trial starts the run its id names, so no run file has to exist first.
