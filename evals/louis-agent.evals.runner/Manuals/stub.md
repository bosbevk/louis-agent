# Stub eval (`stub`): free, no model

## What it measures

Nothing of Puls: it tests the portal itself. The runner writes a fixed run in the portal's protocol, without opening a
session or asking a model, so it costs nothing. Use it to see whether the store, the table, the explorer and the
replay work, before a paid run.

It is the one eval the runner always lists, whatever `EVALS` says.

## The modes

| Mode | Tools (only as a label) |
|---|---|
| `stub-direct` | `web` |
| `stub-delegating` | `specialists`, `web` |

## The conversations

| Conversation | Turns | What it shows |
|---|---|---|
| `bobotie` | 2 (with a follow-up) | thinking, text, and in `stub-delegating` a hand-over to the cook with its own events |
| `cottage-pie` | 1 | the same, one turn |

## The graders

Two fixed rules, both required, so a trial passes only when both do:

| Grader | Checks |
|---|---|
| `no-error` | No turn of the conversation raised an error |
| `within-time` | Every turn was answered within a minute (a stuck run is the only slow one) |

## The scores

| Score | Means |
|---|---|
| `routed_right` | Always yes: the run is fixed |
| `turns` | How many turns the conversation had |

## What it costs

Nothing: no requests, so the cost shows as *—* (unknown: the ledger has nothing for the run).

## By hand

From the portal, press *Run* on its card. Without the portal:

```
dotnet run --project evals/louis-agent.evals.runner -- run stub --repeat 2
```
