# Sample output: the Puls delegation check after FX10–FX12

The third run of `samples/puls-check/run_check.py`, on 2026-10-14 (model `claude-haiku-5-5`, the Puls containers
rebuilt at `bb78ef7`). It asks the same ten questions as the [roles run](../sample-output-puls-check-roles/README.md),
after three fixes that run found:
- [FX10](../../docs/fixes/FX10-optional-peer-name.md): `agent` is optional;
- [FX11](../../docs/fixes/FX11-empty-sub-session-answer.md): an empty answer is asked for once more, in the same session;
- [FX12](../../docs/fixes/FX12-env-templates-readable.md): `.env.example` can be read.

Costs are what the usage ledger recorded (USD), from both containers, specialists included. `answers.md` has each
question's two answers side by side.

## The fixes, checked in the API's tool log

| Fix | Roles run | This run |
|---|---|---|
| FX10: calls refused for a missing `agent`, or sent to a guessed peer | 3 | **0** (all 8 delegations went through first time, none naming `agent`) |
| FX12: "sensitive file is not allowed" | 2 (files-2, both modes) | **0**: both modes listed the settings from `config/.env.example` itself |
| FX11: follow-ups for an empty answer | — | 0: no specialist ended empty, so this fix wasn't exercised |

## The totals

| | Requests | Input | Cache write | Cache read | Output | All tokens | Cost | Time |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| **Baseline** | 27 | 74 | 76,947 | 1,204,856 | 12,511 | 1,294,388 | **$0.0279** | 109 s |
| **Delegated** | 57 | 152 | 424,481 | 1,376,834 | 76,167 | 1,877,634 | **$0.3128** | 401 s |
| · without numbers-2 | 45 | 122 | 128,661 | 342,393 | 30,652 | 501,828 | $0.0348 | 225 s |

Delegated numbers-2 alone cost **$0.278**, 89% of the run. Without it, delegation cost 41% more than the baseline
($0.0348 against $0.0247), and 17% more in the roles run.

## Per question

| Question | Baseline | Delegated | Roles run, delegated | Delegation (toolsets, role) |
|---|---:|---:|---:|---|
| web: the newest .NET | $0.0089 | **$0.0068** | $0.0057 | web, researcher |
| files: summarise the README | $0.0019 | $0.0027 | $0.0032 | files, reader |
| git: the last three commits | $0.0018 | $0.0043 | $0.0043 | git, historian |
| numbers: .cs files and lines | $0.0013 | $0.0036 | $0.0057 | files + bash, analyst |
| chat: Jack Russell names | $0.0008 | **$0.0006** | $0.0006 | none |
| web-2: the prime rate | $0.0059 | $0.0086 | $0.0104 | web, researcher |
| files-2: LLM settings in .env.example | $0.0018 | $0.0031 | $0.0014 (refused) | files, reader |
| git-2: who changed AgentEngine.cs | $0.0017 | $0.0049 | $0.0028 | git + files, historian |
| numbers-2: test methods in core.tests | $0.0032 | **$0.2780** | $0.0166 | **files only, reader** |
| chat-2: a birthday message | $0.0006 | **$0.0002** | $0.0002 | none |

Every delegation passed its role this time (8 of 8; in the roles run 6 of 10 did, and the peer derived the rest).

## What happened on numbers-2

Puls sent a **counting** task to a specialist with **only `files`**, as `puls-reader`. Without Python or bash, the
reader counted by reading every test file into its context, about 74 files in batches of up to 16 calls. The ledger
shows its prompt growing request by request: 7k, 10k, 24k, 52k, 87k, 140k, 180k, 237k, 282k, 298k tokens.

Two things made that expensive:
- **Past 100k tokens, every request is priced at the long-prompt rate**, five times the normal one
  (`config/prices.json`). The last five requests cost $0.26 between them.
- **One request hit the 16,000-token output limit** (`stop: length`). That is the dearest kind of token, at the long
  rate.

The answer was right, and more careful than the baseline's: 725 methods (669 `[Test]` and 56 with `[TestCase]`)
against the baseline's 912 attributes. It took 176 s and cost 87 times as much.

This is a **routing** failure, not one of the fixes. In the roles run, Puls sent the same question to `files` +
`python`. The router's table (`agents-skills.md`) says counting goes to the analyst with a script tool, but nothing
enforces it, and nothing stops a specialist's context from growing past the long-prompt threshold.

## What it says

- **FX10 and FX12 work.** There were no refused calls, no guessed peers, and files-2 was answered from the file itself.
  FX11 is still unconfirmed because nothing ended empty.
- **Delegation still costs more than the baseline** on nine questions (+41%), and is cheaper only for chat and the
  first web question. The baseline was also cheaper this time than in the roles run ($0.0279 against $0.0324): one run
  per mode is noisy.
- **The real risk is the tail, not the average.** One badly routed task cost eight times all the others together. A
  specialist has no limit on how big its context gets.

Possible next fixes (not done; for Louis to choose):
1. **Route counting to a script:** `roles.json` or the peer adds `python` (or `bash`) to a `files`-only session whose
   task counts or totals. Or the reader's role says to hand counting back instead of reading every file.
2. **A context budget per sub-session:** stop or warn as a sub-session's prompt nears the long-prompt threshold
   (100k), and return what it has so far. That bounds the tail, whatever the routing.
3. **More runs per mode** before deciding on delegation by cost, since one run swings by 15–20%.

## Files

| Path | What it is |
|---|---|
| `answers.md` | Each question's two answers side by side, with a *Korreksies* list under each |
| `baseline/`, `delegated/` | `answers.json` (each answer, its tools, delegations with toolsets and role, time), `transcript.md`, `usage.jsonl` |
| `results.json` | The totals: per run, per question, Puls's own and its specialists' |

Totals and `answers.md` again from the saved records: `python samples/puls-check/run_check.py --report samples/sample-output-puls-check-fixes`.
