# 13 · Cost and token economics: what M1 taught

> **Skill:** #13 in the [goal](../GOAL.md)'s map · **Stage:** M1 "See it" (F1–F3, 2026-10-10 to 2026-10-11) · **Next
> stage:** M2 "Cheaper" (F4 caching, F5 toolsets), then estimates, budgets and reconciliation (F6, F7, F11)

## Before and after

**Before M1** nothing read a model's usage. What a chat, a fix or a demo run cost was a guess, worked out by hand from
the provider's console after the fact.

**After M1** every model request of every host is one line in a ledger, priced as it is written, and every place a
person works shows it:

| | Before | After |
|---|---|---|
| A request's tokens | not recorded | input, cache write, cache read and output, per request, in `usage-YYYY-MM.jsonl` (plus reasoning where a provider reports it on its own; Claude counts thinking inside output) |
| Its cost | unknown | priced from a dated table (`config/prices.json`, `as_of`), stored on the record |
| Who it was for | — | host, session, turn, purpose; for the orchestrator, the fix, the run and the service |
| Seen where | the Anthropic console, later | under each answer (CLI, web app, Rider), `/usage`, the API's `message_delta`, the comms log per fix and per run |

**The numbers** (Claude Haiku 5.5, the orchestrator demo):

| Run | Fixes confirmed | Requests | Input tokens | Output tokens | Cost |
|---|---|---|---|---|---|
| Full demo (13 errors): the **M1 baseline** | **10/10** (2 ignored and 1 escalated, as the runbooks say) | 150 | 6,374,282 | 42,244 | **$0.6586** ($0.0625 a fix) |
| Short demo (5 errors), after F3 | **4/4** (1 escalated, as its runbook says) | 57 | 2,444,261 | 16,356 | $0.2526 |

A cost only means something with the quality it bought: a later run that is cheaper because it fixed less is not a
win. Every comparison against this baseline holds the fixes confirmed (10/10) as well as measuring the cost.

And the check that the views can be trusted: the short demo's comms log total and the orchestrator's own records equal
the ledger **exactly**, $0.252605, 57 requests, the same token counts.

## What the numbers say

- **Input is the bill.** 6.37M input tokens against 42K output: 150:1. Output costs 5× input per token on Haiku 5.5
  ($0.50 against $0.10 a million), yet input is about 97% of the cost: $0.637 of $0.659.
- **Most of that input is the same text, again.** Each request re-sends the ~48,000-token system prompt and tool list,
  with a 0% cache-hit rate. A fix is ~11 requests, so the same 48K is paid for ~11 times per fix. This is M2's target:
  prompt caching (F4) reads it at a tenth of the price, and toolset profiles (F5) send less of it. The optimisation
  spec estimates caching at **about −80%** of that prefix's cost per fix (11.4 requests × 48K: ~547K full-price tokens
  down to ~110K, one cache write and then reads); that is the number F4's measurement has to meet.
- **The tool loop multiplies everything.** A turn isn't one request: each tool round is one, so a fix that uses its 10
  rounds and continues in a second message costs about 1.5× a one-message fix on average ($0.077 against $0.053;
  1.44× in the full run, 1.49× in the short one), and up to twice at the extremes ($0.044 to $0.088). The whole run varies far less
  (0.3% between two runs), so M2 is measured on the run total, not on one fix.
- **Triage is cheap; fixes are not.** In the short demo, triage was $0.013 of $0.25. Saving on the orchestrator's
  model would barely show; saving on louis-agent's prompt would.
- **The tokenizer changes the bill, not just the price.** The same prompt was 10,266 tokens on Haiku 4.5 and 15,649 on
  Haiku 5.5 (+52%). Comparing models on price per million tokens alone misses this; comparing them on a run's cost
  doesn't.

## What was built, and why this way

**1. Record at the request, inside the tool loop (F1).** `UsageRecordingChatClient` is middleware inside
Microsoft.Extensions.AI's `FunctionInvokingChatClient`, so each tool round is recorded as its own request.
*Alternative:* record once per turn, outside the loop. Rejected: a turn's total hides which round was expensive, and
providers report usage per request anyway.

**2. Measure the adapter before designing the record (F1's spike).** Before writing the record, a throwaway program
printed every field the Anthropic adapter fills in. It found that `InputTokenCount` *includes* cache reads and writes
(Anthropic's own `input_tokens` doesn't; the adapter adds them back), that cache writes are only in
`AdditionalCounts["CacheCreationInputTokens"]`, and that reasoning is never reported separately. So the ledger stores
four kinds that **don't overlap** (uncached input = input − cache reads − cache writes), and a provider's quirks live in
an `IUsageMapper` (Standard, Anthropic). *Alternative:* store what the adapter gives. That would have priced cached
tokens twice, once as input and once as cache, which would only have shown up once caching was on.

**3. Unknown is never zero (F2).** A model missing from the price table gets `cost: null`, shown as "price unknown"; a
local model priced at 0 shows `$0`; a total with some unpriced requests says how many. *Alternative:* treat missing
prices as 0. Simpler, and wrong in the dangerous direction: a new model would look free.

**4. Store the cost with the record, priced by a dated table (F2).** Each record carries `cost: {currency, amount,
price_table: "2026-10-10"}`. *Alternative:* price at report time. Then a price change rewrites history, and last
month's runs stop matching last month's bill.

**5. Price in a sink decorator, not in the recorder (F2).** `PricingUsageSink` wraps the ledger in
`AgentHost.CreateUsageSink`, where every host's records already pass. Neither the engine nor any host knows about
prices. *Alternative:* price inside the recorder (the feature doc's first idea). Then the engine, and every host that
builds one, would need the price table, and pricing could never be added or changed without touching the tool loop. The cost of that choice showed in F3: the turn's totals need the *priced* record, so `IUsageSink.Record` had
to return what it stored.

**6. One totalling rule for every view (F3).** The CLI line, `/usage`, the API, Rider and the comms log all total with
`UsageReport.Total`; the web app (which can't reference core) repeats the same rules in `UsageFormat`. *Alternative:*
each host adds up its own numbers. Then they drift, and a dashboard that disagrees with the ledger is worse than none:
nobody trusts either. The exact match in the short demo is this decision paying off.

**7. Claude's field names on the wire (F3).** The API's `message_delta.usage` uses Claude's four token fields, summed
over the turn, plus `requests`, `cost`, `currency` and `unpriced`. Any client that already reads Claude's usage reads
ours; the additions say how ours differs (a turn is several requests). *Alternative:* our own names (the ledger's
`input`, `cache_read`…). Every Claude-compatible client would need a mapping, and a bare `input` invites the very
mistake the spike found: is the cached part inside it or not?

## Trade-offs accepted

- **A ledger file per month, not a database.** JSON lines are append-only, readable and easy to total with one tool
  (`usage-probe -- --ledger`); querying across months or by user is F11's job.
- **Two copies of the number rules** (core's `UsageText`, the web app's `UsageFormat`), to keep the agent's code out of
  the browser download. F14 has to change both.
- **The web line splits cache writes from "in"**, against F3's own story: they are priced differently, so hiding them
  would hide why one answer cost more than another.
- **Rider's chat line is opt-in** (`USAGE_IN_CHAT`): in Rider it becomes part of the transcript.
- **Cache writes are priced at the 5-minute rate** until F4 records which lifetime a write used.

## What didn't work (and what it taught)

- **Haiku 5.5 rejected the request after the tool-round limit** (FX1): the loop's last request dropped the tools,
  which a model that binds thinking to the tool list refuses. Measuring the short demo (2 of 4 fixes lost) found it.
- **The ledger lost requests outside a scope** (FX5): `scope?.Add(sink.Record(record))` skips the `Record` call too
  when `scope` is null. F1's tests caught it. Lesson: keep side effects out of a null-conditional call.
- **Failed turns weren't counted in the views** (FX6): their usage only travelled on the success event. A total is only
  as good as its worst path.
- **One timeout ended the orchestrator's whole run** (FX7), because .NET reports a timeout as a cancellation.
- **Local models don't fit yet:** Ollama silently truncates to 2,048 tokens (the ledger showed `input: 2050`), and a
  CPU request can pass the 100-second timeout. The ledger is what made the truncation visible (F13).

## Where skill 13 stands

Against the goal's four criteria for a learned skill:

1. **Built:** the first stage is (ledger, prices, showing it); caching, estimates, budgets and reconciliation are still
   planned (F4, F6, F7, F11).
2. **Measured:** not yet. M1 gives a **baseline**, not a before/after: nothing was cheaper at the end of M1, only
   visible. This baseline is the "before" that F4 is measured against.
3. **Written up:** this page, for M1; extended at M2 and M3.
4. **Explained:** the list below.

So skill 13 is **not learned yet**: it becomes so when a change is measured against this baseline with quality held,
first F4's caching at M2.

## What I can now explain

- Why input tokens, not output, set the bill for an agent, and why caching is the first lever.
- Why a ledger needs non-overlapping token kinds, and how a provider adapter can quietly break that.
- Why a cost is stored with the prices it was charged at, and why "unknown" must never read as "free".
- How to attribute cost across two agents (a fix, its triage, a run) so the totals agree at every level.
- Why the run, not a single fix, is the unit to compare when measuring a change.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md)
