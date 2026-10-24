# Stub check (`stub`): free, no model

## What it measures

Nothing of Puls: it tests the portal itself. The script prints a fixed run in the portal's protocol without opening a
session or asking a model, so it costs nothing. Use it to see whether the store, the table, the explorer and the
replay work, before a paid run.

## The modes

| Mode | Tools (only as a label) |
|---|---|
| `puls-only` | `web` |
| `puls-cook` | `specialists`, `web` |

## The conversations

| Conversation | Turns | What it shows |
|---|---|---|
| `bobotie` | 2 (with a follow-up) | thinking, text, and in `puls-cook` a hand-over to the cook with its own events |
| `cottage-pie` | 1 | the same, one turn |

## The scores

| Score | Means |
|---|---|
| `routed_right` | Always yes: the run is fixed |
| `turns` | How many turns the conversation had |

## What it costs

Nothing: no requests, so the cost shows as *—* (unknown: the ledger has nothing for the run).

## By hand

```
python3 benchmarks/stub/run_check.py --repeat 2
```
