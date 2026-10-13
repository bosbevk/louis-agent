# FX13 · Counting goes to the analyst, and the reader hands it back

> **Status:** done · **Area:** skills: `agents-skills.md` (Puls's routing), `personalities/puls-reader.md` · **Found:** 2026-10-14

## Symptom

In the [delegation check after FX10–FX12](../../samples/sample-output-puls-check-fixes/README.md), question numbers-2
("how many test methods are in core's tests?") cost **$0.278**, 89% of the delegated run. The baseline answered the same
question for $0.0032. It took 176 s and 12 requests.

## Cause

From the API's tool log and the ledger. Puls delegated with `toolsets: ["files"]` and `puls-reader`. Without a script
tool, the reader counted by reading each of the ~74 test files, in batches of up to 16 `ReadWorkspaceFile` calls. Its
prompt grew request by request from 7k to 298k tokens. Past 100k tokens, Haiku 5.5 bills every request at the
long-prompt rate (five times the normal price, `config/prices.json`), so the last five requests cost $0.26.

In the roles run, Puls routed the same question to `files` + `python`. The router's table listed the analyst for
`python`, but nothing said that *counting* needs it. The reader's role told it to read files and never said to stop.

## Fix

Two layers, both prompt content (rule 3 of the layout: prompts are content, not code):
- **The router** (`agents-skills.md`): *"Counting is the analyst's, never the reader's."* Counts, totals and averages
  over more than a few files go to `puls-analyst` with `files` and `python` (or `bash`). It also says why: the reader
  would read every file into its context.
- **The reader** (`puls-reader.md`): *"Don't count by reading."* If a task counts or totals across more than five files,
  the reader stops and reports that it needs the analyst, with the folder and the pattern to count. That costs one
  short sub-session when the routing goes wrong, not 298k tokens.

Added the same day, at Louis's suggestion: **tell the agents what their tool calls cost**, in words that name the
mechanism instead of a general "be efficient":
- **Every specialist role** (all five `puls-*` files, in the shared *How every Puls specialist works*): use the fewest
  tool calls that answer the task, one search or one script rather than reading many files, because every result stays
  in the context and is paid for again on every later request. It goes in the roles because a specialist never loads
  `default.md`, so the reader that read 74 files couldn't have seen a line there.
- **The router** (`agents-skills.md`, read by Puls when it chooses): every sub-session starts cold and its answer is
  written twice, so send one per task, not one per step, with the toolsets that answer in the fewest calls.

Considered and not done:
- **Adding `python` to every `files`-only sub-session on the peer.** Every reader would get a bigger prefix, and its
  role would change for tasks that only read.
- **Deciding from the task text in code** (looking for "how many"). Too brittle, and in two languages.

Code doesn't enforce this, so FX14 (a context budget per sub-session) is the backstop when the prompt isn't followed.

## Tests

`PersonalitiesTests.CountingGoesToTheAnalyst_AndTheReaderHandsItBack` checks that both built-in files carry the rule. `EverySpecialist_IsToldWhatItsToolCallsCost` checks the cost line in each of the five roles.
The golden file changed for the router's lines.

## Confirmed

2026-10-14, in the [three-run delegation check](../../samples/sample-output-puls-check-repeat/README.md): all six counting questions (numbers and numbers-2,
three runs each) went to `files` + `python` with `puls-analyst`, and all 24 delegations matched the router's table.
The dearest delegated question cost $0.0174, against $0.278 before.

## Lesson

Routing is a prompt, so it can fail, and the cost of a wrong route depends on what the wrong specialist does next. Tell
each role what is *not* its job and what to do instead, so a wrong route fails cheaply. Skill 10 (multi-agent
orchestration) and skill 3 (prompt design) in the [goal](../GOAL.md).
