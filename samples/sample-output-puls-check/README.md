# Sample output: the Puls delegation check (baseline vs delegated)

What `samples/puls-check/run_check.py` produced on 2026-10-13 (model `claude-haiku-5-5`, the Puls containers on the F20
branch; run ids `puls-check-baseline-…` and `puls-check-delegated-…`). The same five questions, each in a session of its
own, both times with Puls's personality (`puls`):

- **Baseline:** one agent with every toolset the instance has (not `agents`): it answers everything itself.
- **Delegated:** Puls as built: `agents` only; every task goes to a specialist on `puls-agent` with only its toolsets.

Costs are what the usage ledger recorded (USD), both containers' records, specialists included.

## The totals

| | Requests | Input | Cache write | Cache read | Output | All tokens | Cost |
|---|---:|---:|---:|---:|---:|---:|---:|
| **Baseline** | 13 | 36 | 66,298 | 561,937 | 5,527 | 633,798 | **$0.0167** |
| **Delegated** | 22 | 62 | 103,781 | 136,908 | 14,947 | 255,698 | **$0.0218** |
| · Puls itself | 10 | 30 | 12,248 | 37,747 | 6,188 | 56,213 | $0.0050 |
| · its specialists | 12 | 32 | 91,533 | 99,161 | 8,759 | 199,485 | $0.0168 |
| **Change** | +69% | | +57% | −76% | **+170%** | **−60%** | **+31%** |

## Per question

| Question | Baseline tokens | Delegated tokens | Baseline cost | Delegated cost | Baseline time | Delegated time |
|---|---:|---:|---:|---:|---:|---:|
| web: the newest .NET, with sources | 301,014 | 51,687 | $0.0100 | **$0.0051** | 23.6 s | 23.8 s |
| files: summarise the README | 96,768 | 30,554 | $0.0019 | $0.0029 | 5.2 s | 12.1 s |
| git: the last three commits | 94,066 | 51,430 | $0.0019 | $0.0047 | 15.0 s | 31.2 s |
| numbers: .cs files and lines in core | 95,880 | 117,051 | $0.0019 | $0.0087 | 6.9 s | 36.4 s |
| chat: three names for a Jack Russell | 46,070 | 4,976 | $0.0009 | **$0.0005** | 7.8 s | 7.3 s |

## What it says

- **Delegation uses 60% fewer tokens but costs 31% more, and tool tasks take about twice as long.** Tokens alone would
  call it a win; the token *kinds* say why it isn't:
  - **The baseline is mostly cache reads** (562k of 634k), the cheapest kind: one big prefix (~45k tokens, every
    toolset), written once and read by every later session with the same toolsets.
  - **Each specialist starts cold:** a new session with its own toolset profile writes its prefix (cache write, dearer
    than input) and rarely reuses it: 91.5k of the delegated run's 103.8k cache writes are the specialists'.
  - **Output nearly triples** (5.5k → 14.9k): the specialist writes its report, then Puls writes the answer again in its
    own voice. Output is the dearest kind.
- **Where delegation wins:** the web question (half the cost: the baseline's big prefix was read six times over a long
  research turn) and plain chat (Puls's ~2k-token prefix against ~45k).
- **Quality:** both runs answered all five. Delegated web was more current (the 10.0.13 patch of the day, by checking
  the date); delegated numbers were rounder ("about 11,000" against the baseline's exact 10,971); Puls gave seven dog
  names when asked for three (in both runs it's Puls's own voice).
- **Roles were passed once in four delegations** (from the API's tool log): `puls-historian` for git; none for web,
  files or numbers, so those specialists ran with the default personality. The toolsets were right every time (numbers
  got `files` + `python`, as the router says).

## Against F5's *Done when*

F5 keeps delegation as the default "only for tasks where the ledger shows it is cheaper". On these five, the ledger
shows it cheaper for **web research and plain chat**, dearer for **files, history and numbers**. A decision for Louis.
Ways to close the gap, each to be measured the same way:

1. Answer short file, history and number questions without delegating (Puls gets `files` and `git` itself), keeping
   delegation for research and long tasks.
2. Keep specialists warm: fewer, fixed profiles per role so their prefixes stay cached across tasks.
3. Pass the specialist's report through instead of rewriting it, when it's already in the user's language.
4. Make the role reliable: derive it from the toolsets when Puls doesn't pass one (code, not the prompt).

## Files

| Path | What it is |
|---|---|
| `baseline/`, `delegated/` | `answers.json` (each answer, its tools, time), `transcript.md`, `usage.jsonl` (the run's ledger records, priced) |
| `results.json` | The totals above, per run, per question, Puls's own and its specialists' |

Totals again from the saved records (no model calls): `python samples/puls-check/run_check.py --report samples/sample-output-puls-check`.
The script now also records each delegation's toolsets and role (`delegations`); this run's roles come from the API's
tool log, as the first run's script missed them.
