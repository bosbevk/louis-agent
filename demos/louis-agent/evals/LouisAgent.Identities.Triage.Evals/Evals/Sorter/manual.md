# Sorter eval (`triage-sorter`): the triage team's sorter, alone

## What it measures

Does the sorter send an error to the department that owns the code that threw? One error of the made-up order
service (`demos/louis-agent/samples/order-service`): `order-total 1003` throws a `KeyNotFoundException` for the retired
discount code `SUMMER25`, in the service's own code. Right is `SortCase` with `code`.

The session opens as the sorter itself, at a specialist's level (`"depth": 1`), with only the `triage` toolset, as
the manager will open it. Its brief names the run id and the departments, as the manager's would.

## The checks

| Check | Passes when |
|---|---|
| `sorter-no-error` | The turn ran without an error |
| `tool-used` | The sorter called `SortCase` |
| `tool-used-with` | It called `SortCase` with the department `code` |

## Running it

The first run records the sorter's answer and spends the model key, so it waits for Louis's go; every run after
replays the recording for nothing. The coding demo's runner lists it (`EVALS=triage`). An eval's trial starts the run
its id names (F106.3.2 step 7), so no run file has to exist first.
