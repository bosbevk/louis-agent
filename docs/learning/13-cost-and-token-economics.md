# 13 · Cost and token economics: what M1 and F4 taught

> **Skill:** #13 in the [goal](../GOAL.md)'s map · **Stages:** M1 "See it" (F1–F3, 2026-10-10 to 2026-10-11), then
> the first of M2 "Cheaper": F4 prompt caching (2026-10-11); F18's replay cache (2026-10-29) · **Next:** estimates,
> budgets and reconciliation (F6, F7, F11)

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
  spec estimates caching at **about −80%** of that prefix's cost per fix (11.4 requests × 48K: ~547K full-price
  tokens down to ~110K, one cache write and then reads); that is the number F4's measurement has to meet.
- **The tool loop multiplies everything.** A turn isn't one request: each tool round is one, so a fix that uses its 10
  rounds and continues in a second message costs about 1.5× a one-message fix on average ($0.077 against $0.053;
  1.44× in the full run, 1.49× in the short one), and up to twice at the extremes ($0.044 to $0.088). The whole run
  varies far less (0.3% between two runs), so M2 is measured on the run total, not on one fix.
- **Triage is cheap; fixes are not.** In the short demo, triage was $0.013 of $0.25. Saving on the orchestrator's
  model would barely show; saving on louis-agent's prompt would.
- **The tokenizer changes the bill, not just the price.** The same prompt was 10,266 tokens on Haiku 4.5 and 15,649 on
  Haiku 5.5 (+52%, measured in the optimisation spec,
  [§A](../specs/RESPONSE_OPTIMISATION.md#a-measure-usage-prerequisite-for-everything-else)). Comparing models on price
  per million tokens alone misses this; comparing them on a run's cost doesn't.

## What was built, and why this way

**1. Record at the request, inside the tool loop (F1).** `UsageRecordingChatClient` is middleware inside
Microsoft.Extensions.AI's `FunctionInvokingChatClient`, so each tool round is recorded as its own request.
*Alternative:* record once per turn, outside the loop. Rejected: a turn's total hides which round was expensive, and
providers report usage per request anyway.

**2. Measure the adapter before designing the record (F1's spike).** Before writing the record, a throwaway program
printed every field the Anthropic adapter fills in. It found that `InputTokenCount` *includes* cache reads and writes
(Anthropic's own `input_tokens` doesn't; the adapter adds them back), that cache writes are only in
`AdditionalCounts["CacheCreationInputTokens"]`, and that reasoning is never reported separately. So the ledger stores
four kinds that **don't overlap** (uncached input = input − cache reads − cache writes), and a provider's quirks live
in an `IUsageMapper` (Standard, Anthropic). *Alternative:* store what the adapter gives. That would have priced cached
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
builds one, would need the price table, and pricing could never be added or changed without touching the tool loop.
The cost of that choice showed in F3: the turn's totals need the *priced* record, so `IUsageSink.Record` had to return
what it stored.

**6. One totalling rule for every view (F3).** The CLI line, `/usage`, the API, Rider and the comms log all total with
`UsageReport.Total`; the web app (which can't reference core) repeats the same rules in `UsageFormat`. *Alternative:*
each host adds up its own numbers. Then they drift, and a dashboard that disagrees with the ledger is worse than none:
nobody trusts either. The exact match in the short demo is this decision paying off.

**7. Claude's field names on the wire (F3).** The API's `message_delta.usage` uses Claude's four token fields, summed
over the turn, plus `requests`, `cost`, `currency` and `unpriced`. Any client that already reads Claude's usage reads
ours; the additions say how ours differs (a turn is several requests). *Alternative:* our own names (the ledger's
`input`, `cache_read`…). Every Claude-compatible client would need a mapping, and a bare `input` invites the very
mistake the spike found: is the cached part inside it or not?

## F4: the first measured change

M1 made the cost visible; F4 is the first change measured against it, with quality held:

| Full demo, Claude Haiku 5.5 | M1 baseline | With caching (F4) | Change |
|---|---:|---:|---:|
| Fixes confirmed | 10/10 | 10/10 | held |
| louis-agent's input cost | $0.6081 | $0.0820 | **−87%** |
| Average fix | $0.0625 | $0.0099 | −84% |
| The run (triage not cached) | $0.6586 | $0.1334 | **−80%** |
| Share of louis-agent's prompt read from the cache | 0% | 98% | |

The target was −60% on input; the spec's estimate for the prefix was −80%, and the measurement says −88%. The run is
the unit, as M1 taught: one fix still varies ($0.0075 to $0.0146), the run total doesn't move much.

**What made the difference, and why this way:**

**8. Two markers, and the second one placed by the API.** Marker 1 sits at the end of the system prompt and covers
tools and system (the order is tools → system → messages); marker 2 is automatic caching, a field on the request that
the API puts on the last block and moves forward every round. *Alternative:* an explicit marker on a clone of the
newest message (the feature doc's first design). More control, but the clone is where bugs hide, and it is code we
don't need: the API already knows where the last block is.

**9. Mark a copy, never the history.** The SDK's `WithCacheControl` changes the block it is called on. Called on the
history's system prompt, every request would add another marker (past the API's limit of 4) and, worse, edit history
that Haiku 5.5 binds its thinking to. A test fails if the history's own blocks are marked.

**10. A lifetime per marker, calculated.** A cache entry lives 5 minutes or 1 hour after its last use; writing a 1-hour
entry costs 2× instead of 1.25×. Which is cheaper depends only on the gaps between requests that reuse the entry, so
the ledger, which records every request's time, decides it: `usage-probe -- --cache-gaps` replays the real timeline
against both lifetimes. The conversation is always 5 minutes (only the next round reads it); the prefix takes
`LLM_CACHE_TTL`, 5 minutes for the demo (every gap is under 5 minutes) and 1 hour for a chat with pauses. *Alternative:*
pick one lifetime by feel. A 1-hour default would have cost the demo more on every write.

**11. Caching as a provider strategy.** `IPromptCachingStrategy` (Anthropic's markers, or none) chosen once in the
factory, as `IUsageMapper` is. The client that applies it doesn't know which provider it is talking to.

**What the measurements showed that the design didn't predict:**

- **The prefix is shared across sessions.** It was written once per run (47,424 tokens), then *read* by each of the
  nine later fixes, each in its own session. The cache key is the bytes, not the conversation.
- **The split of writes by lifetime is only in the raw response.** Microsoft.Extensions.AI's `UsageDetails` has the
  total; the 5-minute and 1-hour parts are on the SDK's own message, and on a stream only in the first event
  (`message_start`). Pricing 1-hour writes correctly needed the mapper to see that event.
- **The tool order had to be fixed first.** Tools were registered in reflection order; any difference in order between
  processes would have been a different prefix and a silent miss. They are now sorted by name.
- **A miss raises no error.** Caching that silently stops looks like a normal, more expensive run, so the smoke check is
  kept as a sample to compare `cache_read` against.

## Trade-offs accepted

- **A ledger file per month, not a database.** JSON lines are append-only, readable and easy to total with one tool
  (`usage-probe -- --ledger`); querying across months or by user is F11's job.
- **Two copies of the number rules** (core's `UsageText`, the web app's `UsageFormat`), to keep the agent's code out of
  the browser download. F14 has to change both.
- **The web line splits cache writes from "in"**, against F3's own story: they are priced differently, so hiding them
  would hide why one answer cost more than another.
- **Rider's chat line is opt-in** (`USAGE_IN_CHAT`): in Rider it becomes part of the transcript.
- **Triage is not cached yet** (F17): about a quarter of the cached run's cost is now the orchestrator's triage,
  because louis-agent's share fell so far.
- **`--cache-gaps` estimates the prefix** as a host's smallest prompt: an upper bound, about 1,000 tokens high on the
  demo, close enough to choose a lifetime.

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

## F18: the replay cache, measured (2026-10-29)

An eval run costs money every time, because it asks the model. So F18 can record a run's model answers and play them
back. A hit is priced as a known $0 in the ledger, never left blank: "free" and "unknown" are different things.

| | Live run | Replayed |
|---|---|---|
| `puls-cook`, 3 repeats | $0.2167, 259 requests | repeat 1 replayed: $0, 74 requests, every one a hit |
| `puls-routing`, 3 repeats | $0.1561, 174 requests | repeat 1 replayed: $0, 62 requests, every one a hit |

So the hit rate on what was asked was 100%, and the replays cost nothing. The cook replay's 13,068 tokens are the local
judge grading the answers again, which costs nothing either. Both live runs together cost $0.37: an eval of this size
is cheap enough to run often, and a replay is free enough to run on every change to the graders.

One thing a replay can't do yet. When Puls hands a meal to its cook, the hand-off's tool result isn't exactly the same
in the replay as in the recording, so the next request's key is different and the cache has no answer for it. Those
turns fail instead of costing money, so the safety holds (a miss never calls the model), but a mode that delegates
can't reproduce its grades from a replay. That's the next fix: make the sub-session's result replay-stable.

## Where skill 13 stands

Against the goal's four criteria for a learned skill:

1. **Built:** seeing the cost (ledger, prices, showing it, M1) and cutting it (prompt caching, F4); toolsets, estimates,
   budgets and reconciliation are still planned (F5, F6, F7, F11).
2. **Measured:** yes. F4 against the M1 baseline: −80% on the run, −87% on louis-agent's input, with 10/10 fixes held.
3. **Written up:** this page, for M1 and F4; extended at the rest of M2 and at M3.
4. **Explained:** the list below.

So skill 13 is **learned at its first measured change**, by this page's own test. The later stages (spend less of the
prompt, estimate before spending, stop at a budget, reconcile with the bill) extend it.

## What I can now explain

- Why input tokens, not output, set the bill for an agent, and why caching is the first lever.
- Why a ledger needs non-overlapping token kinds, and how a provider adapter can quietly break that.
- Why a cost is stored with the prices it was charged at, and why "unknown" must never read as "free".
- How to attribute cost across two agents (a fix, its triage, a run) so the totals agree at every level.
- Why the run, not a single fix, is the unit to compare when measuring a change.
- How prompt caching prices a request: what a marker covers, why the order tools → system → messages matters, and why
  a write costs more than a read.
- Why a cache lifetime is chosen from the gaps between requests, not from how long a run takes, and how to measure it.
- Why a cache key is bytes, so a stable tool order and an untouched history are what make the cache hit.
- Why a replayed answer is a known $0 and not a blank, and why a replay miss must fail rather than call the model.

---
[Learning write-ups](README.md) · [Goal](../GOAL.md)
