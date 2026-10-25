# Routing check (`routing`): one agent against Puls as built

## What it measures

Does Puls send each kind of question to the right place? Twelve questions, two of each kind, each in its own session.
Only the cook is on (F29), so a meal must go to `puls-cook`, and every other question is right when Puls answers it
itself: the web with its own `web`, the rest by saying what it can't do yet. What is counted is the call made, not the
words. The cost per mode comes from the ledger.

## The modes

| Mode | Tools | What it is |
|---|---|---|
| `baseline` | every toolset this instance has, except `specialists` and `minions` | one agent that does everything itself and can hand nothing over |
| `delegated` | `specialists`, `web` | Puls as built; the cook that gets a question gets only `web` |

The `baseline`'s toolsets come from `GET /v1/toolsets` when the run starts: Paymo and DevOps are among them if their keys
are set.

## The questions

| Kind | Questions | Right in `delegated` |
|---|---|---|
| `web`, `web-2` | The latest .NET; the prime lending rate | Puls itself |
| `files`, `files-2` | Summarise README.md; the LLM settings in .env.example | Puls itself |
| `git`, `git-2` | The last three commits; who changed AgentEngine.cs most | Puls itself |
| `numbers`, `numbers-2` | How many .cs files and lines; how many test methods | Puls itself |
| `chat`, `chat-2` | Names for a Jack Russell; a birthday message | Puls itself |
| `cook`, `cook-2` | Dinner for four in Stellenbosch; dinner for two in Pretoria | `puls-cook` |

## The scores

| Score | Means | ✓ when |
|---|---|---|
| `role` | The role Puls handed over to, or empty if Puls answered itself | — |
| `expected_role` | The role that would be right: `puls-cook` for food in `delegated`, otherwise empty | — |
| `routed_right` | `role` is `expected_role`, and the turn didn't fail | yes |
| `answered_itself` | Puls answered itself, without an error | — |
| `delegations` | How many times Puls handed over (`StartSubSession` or `ContinueSubSession`) | 0 or 1 |
| `puls_calls` | Puls's own tool calls | — |
| `specialist_calls` | The specialist's calls | — |
| `hosts` | The sites pages were read from | — |
| `errors` | 1 if the turn failed | 0 |

The `baseline` can't hand over, so it is right unless it failed: it measures what the same questions cost without
Puls's hand-over.

## What it costs

It spends money: 24 questions per repeat on Claude. The `baseline`'s prompt is bigger (all its toolsets' guides), so
each question costs more there; the first paid run's real cost goes here.

## How to read it

- **On the page:** under each question it says whether it was routed right.
- **In the explorer:** the score (*n / 12 routed right*) and cost per mode on the tiles, then per question side by
  side, then the turn itself, replayed as it was typed.

## By hand

```
BENCHMARK_RUN=local PULS_API=http://127.0.0.1:5070/ python3 benchmarks/routing/run_check.py --repeat 1
```
