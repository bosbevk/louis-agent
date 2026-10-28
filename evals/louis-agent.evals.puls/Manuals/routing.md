# Routing eval (`puls-routing`): one agent against Puls as built

## What it measures

Does Puls send each kind of question to the right place? Eighteen questions, each in its own session: twelve in the
`dev` split (two of each kind) and six held out (one of each). Only the cook is on (F29), so a meal must go to
`puls-cook`, and every other question is right when Puls answers it itself: the web with its own `web`, the rest by
saying what it can't do yet. What is counted is the call made, not the words. The cost per mode comes from the ledger.

A run asks for the `dev` questions unless you say `--split held-out` or `--split all`.

## The modes

| Mode | Tools | What it is |
|---|---|---|
| `baseline` | every toolset this instance has, except `specialists` and `minions` | one agent that does everything itself and can hand nothing over |
| `delegated` | `specialists`, `web` | Puls as built; the cook that gets a question gets only `web` |

The `baseline`'s toolsets come from `GET /v1/toolsets` when the run starts: Paymo and DevOps are among them if their keys
are set.

## The questions

| Kind | Questions (dev, then held out) | Right in `delegated` |
|---|---|---|
| `web` | The latest .NET; the prime lending rate; the rand against the dollar | Puls itself |
| `files` | Summarise README.md; the LLM settings in .env.example; the goal in docs/GOAL.md | Puls itself |
| `git` | The last three commits; who changed AgentEngine.cs most; the files changed most in five commits | Puls itself |
| `numbers` | How many .cs files and lines; how many test methods; how many Markdown files in docs | Puls itself |
| `chat` | Names for a Jack Russell; a birthday message; a name for a black cat | Puls itself |
| `cook` | Dinner for four in Stellenbosch; dinner for two in Pretoria; dinner for six, one vegetarian | `puls-cook` |

## The graders

One fixed rule, required:

| Grader | Checks |
|---|---|
| `routed-to` | The first turn went where the scenario expects (`routes_to`): the cook for food, otherwise Puls itself. A turn that errored is not right, and a specialist's own hand-over is not the turn's route |

There is no `no-error` grader: a one-turn question is routed right only without an error.

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

The scores are the keys the Python check wrote, kept until step 7c empties the list; the grade above is what a trial
passes on. The `baseline` can't hand over, so it is right unless it failed: it measures what the same questions cost
without Puls's hand-over.

## What it costs

It spends money: every question is a turn on Claude. The `baseline`'s prompt is bigger (all its toolsets' guides), so
each question costs more there. The run's own cost is in the ledger and on its page.

## How to read it

- **On the page:** under each question it says whether it was routed right.
- **In the explorer:** the score (*n / 12 routed right*) and cost per mode on the tiles, then per question side by
  side, then the turn itself, replayed as it was typed.

## By hand

With the Puls containers up, from the repo's root:

```
EVALS=puls BENCHMARK_RUN=local AGENT_API=http://127.0.0.1:5070/ dotnet run --project evals/louis-agent.evals.runner -- run puls-routing --repeat 1
```
