# Sample output: the Puls delegation check, with roles (ten questions)

The second run of `samples/puls-check/run_check.py`, on 2026-10-13 (model `claude-haiku-5-5`, the Puls containers on the
F20 branch). It ran after commit 4a8ba3f: **a specialist without a role gets one from its toolsets** (`roles.json`, on
the peer). It asks ten questions, two of each kind, to compare with the [first run](../sample-output-puls-check/README.md)
(five questions, no derived roles). Each question gets its own session, and both runs use Puls's personality:

- **Baseline:** one agent with every toolset the instance has (not `agents`), which answers everything itself.
- **Delegated:** Puls as built: `agents` only, and every task goes to a specialist on `puls-agent`.

Costs are what the usage ledger recorded (USD), from both containers, specialists included. **`answers.md` puts the
two answers to each question side by side**, with room for corrections to the Afrikaans.

## The totals

| | Requests | Input | Cache write | Cache read | Output | All tokens | Cost |
|---|---:|---:|---:|---:|---:|---:|---:|
| **Baseline** | 30 | 80 | 92,624 | 1,361,625 | 14,356 | 1,468,685 | **$0.0324** |
| **Delegated** | 67 | 172 | 165,326 | 665,017 | 47,176 | 877,691 | **$0.0509** |
| · Puls itself | 24 | 68 | 23,544 | 99,935 | 12,864 | 136,411 | $0.0104 |
| · its specialists | 43 | 104 | 141,782 | 565,082 | 34,312 | 741,280 | $0.0405 |
| **Change** | +123% | | +78% | −51% | **+229%** | **−40%** | **+57%** |

Time: 154 s for the baseline, 344 s delegated (2.2×).

## Per question

| Question | Baseline cost | Delegated cost | Baseline time | Delegated time | Role (how) |
|---|---:|---:|---:|---:|---|
| web: the newest .NET, with sources | $0.0090 | **$0.0057** | 19.5 s | 34.9 s | researcher (derived) |
| files: summarise the README | $0.0020 | $0.0032 | 6.1 s | 14.9 s | reader (passed; 2 failed calls first) |
| git: the last three commits | $0.0020 | $0.0043 | 12.4 s | 30.4 s | historian (passed) |
| numbers: .cs files and lines in core | $0.0024 | $0.0057 | 10.5 s | 36.4 s | analyst (passed; 1 failed call first) |
| chat: three names for a Jack Russell | $0.0008 | **$0.0006** | 9.2 s | 8.9 s | none (Puls answers) |
| web-2: South Africa's prime rate | $0.0052 | $0.0104 | 27.9 s | 63.2 s | researcher (passed) |
| files-2: the LLM settings in .env.example | $0.0028 | **$0.0014** | 14.6 s | 19.8 s | reader (derived) |
| git-2: who changed AgentEngine.cs most | $0.0047 | **$0.0028** | 35.0 s | 26.5 s | historian (derived) |
| numbers-2: test methods in core.tests | $0.0030 | $0.0166 | 15.5 s | 105.5 s | analyst (passed; asked twice) |
| chat-2: a birthday message for my mom | $0.0006 | **$0.0002** | 3.3 s | 3.0 s | none (Puls answers) |

Delegation was cheaper on 5 of 10. The roles come from the API's tool log; `delegated/answers.json` has them under
`delegations`. This run's script had not yet captured them, so `transcript.md` shows `None`.

## What it says

- **The same picture as the first run, now on ten questions:** 40% fewer tokens, 57% more cost. The baseline is mostly
  cache reads (1.36M of 1.47M). Each specialist starts cold and writes its own prefix (142k of the 165k cache writes).
  Output more than triples, because the specialist writes a report and then Puls writes the answer again.
- **Roles now work: every delegation ran with a role.** Puls passed one in 6 of the 10 delegated questions (first run:
  1 in 4). The peer derived the other 4 from `roles.json`, which is the fix measured here.
- **Delegation wins where it won before (chat, one web question), and on two quick lookups** (files-2, git-2), where the
  baseline's big prefix costs more than a small specialist.
- **Most of the extra cost comes from four problems, not from delegation itself:**
  1. **`agent` is a required parameter.** Puls left it out twice (the call failed) and once guessed `default` (no such
     peer). Puls has only one peer, so `agent` could be optional.
  2. **numbers-2 asked twice.** The first sub-session returned only its usage line and no answer, so Puls sent the task
     again. That question alone cost $0.0166, a third of the delegated run.
  3. **`config/.env.example` is blocked as a sensitive file** in both runs, so neither could answer files-2 from the
     file itself (the baseline guessed the settings from the code). An example file holds no secrets.
  4. **Web search was blocked** in both runs (the search provider refused), so both read the sources directly.
- **Quality:** the two runs disagree on numbers-2: 721 test methods (baseline) against 718 (delegated). Both found 901
  attribute lines (665 `[Test]` + 236 `[TestCase]`), so the counts differ only in how a method with several `[TestCase]`s
  is counted. Delegated web-2 was more useful: it found the September 2026 repo-rate change, while the baseline only
  confirmed July 2025.

## Against F5's *Done when*

If numbers-2 had cost what numbers did ($0.0057), the delegated run would cost about $0.040, 23% above the
baseline's $0.032: dearer and twice as slow overall, but cheaper on chat, research and quick lookups. Ways to close the gap, each to
be measured the same way:

1. Make `agent` optional (the only peer when there is one).
2. Treat a sub-session that returns no answer as an error the specialist sees, not an empty result.
3. Let `.env.example` files through `SensitiveFiles` (only the real `.env` files hold secrets).
4. From the first run: keep specialists warm, and pass a report through when it's already in the user's language.

## Files

| Path | What it is |
|---|---|
| `answers.md` | Each question's two answers side by side, with a *Korreksies* list under each |
| `baseline/`, `delegated/` | `answers.json` (each answer, its tools, delegations, time), `transcript.md`, `usage.jsonl` (the run's priced ledger records) |
| `results.json` | The totals above: per run, per question, Puls's own and its specialists' |

Totals and `answers.md` again from the saved records (no model calls):
`python samples/puls-check/run_check.py --report samples/sample-output-puls-check-roles`.
