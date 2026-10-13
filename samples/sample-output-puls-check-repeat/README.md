# Sample output: the Puls delegation check, three runs per mode (after FX10–FX14)

The fourth check (`samples/puls-check/run_check.py --repeat 3`), on 2026-10-14, with model `claude-haiku-5-5` and the
Puls containers rebuilt at `3c8ee04`. It asks the same ten questions as the
[roles run](../sample-output-puls-check-roles/README.md), three times in each mode, after five fixes:

| Fix | What it changed |
|---|---|
| [FX10](../../docs/fixes/FX10-optional-peer-name.md) | `agent` is optional, so calls are no longer refused |
| [FX11](../../docs/fixes/FX11-empty-sub-session-answer.md) | An empty answer is asked for again, in the same session |
| [FX12](../../docs/fixes/FX12-env-templates-readable.md) | `.env.example` can be read |
| [FX13](../../docs/fixes/FX13-counting-to-the-analyst.md) | Counting goes to the analyst, and every agent is told what tool calls cost |
| [FX14](../../docs/fixes/FX14-sub-session-prompt-budget.md) | A sub-session's prompt has a 100k-token budget |

The three runs cost **$0.19** together. `summary.md` has the table below; each `run-N/` has its own answers, transcripts,
usage records and `answers.md` (both answers side by side).

## Three runs per mode

| | Baseline | Delegated | Delegated vs baseline |
|---|---:|---:|---:|
| Run 1 | $0.0336 | $0.0355 | +6% |
| Run 2 | $0.0243 | $0.0269 | +11% |
| Run 3 | $0.0250 | $0.0464 | +86% |
| **Mean** | **$0.0276** | **$0.0363** | **+31%** |
| Tokens, mean | 1,411,137 | 576,211 | −59% |

| Question | Baseline: mean (lowest–highest) | Delegated: mean (lowest–highest) |
|---|---:|---:|
| web | $0.0061 (0.0033–0.0105) | **$0.0037** (0.0026–0.0053) |
| files | $0.0020 (0.0020–0.0020) | $0.0024 (0.0019–0.0030) |
| git | $0.0019 (0.0017–0.0023) | $0.0030 (0.0024–0.0043) |
| numbers | $0.0014 (0.0014–0.0014) | $0.0025 (0.0020–0.0035) |
| chat | $0.0008 (0.0008–0.0009) | **$0.0005** (0.0003–0.0006) |
| web-2 | $0.0081 (0.0075–0.0094) | **$0.0078** (0.0053–0.0123) |
| files-2 | $0.0019 (0.0018–0.0019) | $0.0032 (0.0024–0.0038) |
| git-2 | $0.0019 (0.0017–0.0022) | $0.0030 (0.0022–0.0039) |
| numbers-2 | $0.0029 (0.0026–0.0032) | $0.0100 (0.0055–0.0174) |
| chat-2 | $0.0006 (0.0006–0.0006) | **$0.0002** (0.0002–0.0002) |

## The fixes, checked

- **FX10 and FX12:** no refused calls, and every files-2 answer was read from `config/.env.example`.
- **FX13, the routing:** all 24 delegations went to the role the router names. Counting (numbers, numbers-2) went to
  `files` + `python` with `puls-analyst` all six times; the run before went to a `files`-only reader. Every delegation
  passed its role.
- **FX11, confirmed in run 3:** the numbers-2 analyst used all ten tool rounds and ended without text (turn 1, `stop`
  with 170 output tokens). The follow-up in the same session (turn 2) gave the answer, and Puls called
  `StartSubSession` once. In the roles run the same thing started a second sub-session from scratch.
- **FX14, the budget:** not reached. The largest sub-session prompt in the three runs was 46k tokens,
  far from 100k. With the routing fixed, nothing grew near it.
- **The tail is gone:** the dearest delegated question cost $0.0174, against $0.278 in the run before the fixes.

## What it says

- **Delegation costs about a third more on average** (+31%), with a wide spread: +6%, +11% and +86%. Run 3's extra
  cost is web-2 ($0.0123) and numbers-2 ($0.0174, the ten-round analyst). One run per mode would have shown anything
  from "about the same" to "almost double".
- **It is cheaper for chat and for the first web question in every run**, and about even for web-2. Those are the
  tasks where the baseline's big prefix (every toolset, ~45k tokens) costs more than a small specialist.
- **It costs 1.2–3.5× more for short files, git and counting questions:** the specialist starts cold, and the answer
  is written twice.
- **Delegation is slower**: the baseline answers a short files or git question in 6–12 s, the delegated run in 11–38 s.

That supports the TODO's *relook at how often Puls delegates*: keep delegating research and long or risky work, and
let Puls answer short read-only questions itself. That's for Louis to decide, and to measure the same way.

## Files

| Path | What it is |
|---|---|
| `summary.md`, `summary.json` | Each mode's and each question's cost over the three runs: mean, lowest, highest |
| `run-N/` | One run: `answers.md` (side by side), `results.json`, and per mode `answers.json`, `transcript.md`, `usage.jsonl` |

Run it again: `python samples/puls-check/run_check.py <out> --repeat 3` (about 25 minutes, about $0.20).
