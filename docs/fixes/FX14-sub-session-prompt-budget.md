# FX14 · A sub-session's prompt has a budget

> **Status:** done · **Area:** core, the tool loop (`Agent/ToolCallGuards`), `SUB_SESSION_PROMPT_BUDGET` · **Found:** 2026-10-14

## Symptom

The same run as [FX13](FX13-counting-to-the-analyst.md): delegated numbers-2 cost **$0.278**, eight times the other nine
questions together. The specialist's prompt grew by every file it read, request by request: 7k, 10k, 24k, 52k, 87k,
140k, 180k, 237k, 282k and 298k tokens. One request also hit the 16,000-token output limit.

## Cause

Every tool result stays in the conversation, and every later request sends it again. Past 100,000 tokens, Haiku 5.5
prices the **whole request** at the long-prompt rate, five times the normal one (`config/prices.json`). The last five
requests cost $0.26 between them. Nothing limited how large a sub-session's prompt could get: only the 10-round limit,
and ten rounds of 16 file reads each are plenty to pass 100k.

FX13 fixes the routing that started it, but routing is a prompt and can still fail. The cost of a failure needs a
limit in code.

## Fix

A budget on a sub-session's prompt: `SUB_SESSION_PROMPT_BUDGET`, default **100,000** tokens (the long-prompt
threshold); 0 turns it off. Before each request in a sub-session's tool loop, `ToolCallGuards.KeepWithinPromptBudget`
estimates the next prompt as the last prompt plus its last growth, read from the turn's usage records
(input + cache reads + cache writes). If that estimate reaches the budget, the request keeps its tools with
`ChatToolMode.None` (Anthropic `tool_choice: none`), the same way the loop's final request works since FX1. The model
can't call another tool, so it answers from what it has, and the loop ends.

The decisions behind it:
- **Only sub-sessions** (the turn's usage tags name a parent turn). A user's own session may need a big context, and
  the user sees its cost as it grows. A specialist runs unseen.
- **The tool list stays the same**, only the mode changes: Haiku 5.5 binds its thinking to the tool list (FX1, FX4).
- **The last growth predicts the next request**, so the forced answer usually lands *under* the budget instead of
  just over it. It's an estimate: one unusually large result can still pass it, once.
- **From the ledger's own records** (`UsageScope.Records`), so the budget counts the tokens the provider reported, not
  a character estimate. Summary requests run outside the loop and aren't counted.
- **The answer is partial, not an error.** The specialist reports what it found, and its role already says to name
  what's missing. FX11's follow-up only fires on an empty answer.
- A line in the log: `[TOOL] Sub-session prompt budget: … the next request answers without tools`.

Considered and not done:
- **Stopping the loop** (`FunctionInvocationContext.Terminate`). The turn would end on a tool result with no text, and
  FX11 would then ask again at the same large prompt.
- **Trimming old tool results.** That edits the history, which Haiku 5.5's thinking binding forbids (FX4).
- **A cost budget in dollars.** It needs prices inside the loop. Tokens are what grows, and the threshold is in tokens.

## Tests

`SubSessionPromptBudgetTests`:
- the prompt grows 300, 600, 900 tokens; with a budget of 1,000, the fourth request has tool mode None and the same tools;
- a session without a parent (the user's own) has no budget;
- a budget of 0 is off;
- `SUB_SESSION_PROMPT_BUDGET` is parsed (unset or invalid gives the default).

## Confirmed

Partly, on 2026-10-14. In the [three-run delegation check](../../samples/sample-output-puls-check-repeat/README.md) no sub-session prompt went over 46k tokens, so
the budget was never reached and the guard's forced answer hasn't been seen against the real model yet. The tests
cover the mechanism. A real confirmation needs a task that grows a specialist's context past 100k.

## Lesson

Limit the tail in code. A prompt can make a failure less likely, but only a limit decides what a failure costs. Context
grows with every tool call, and long-context pricing turns that growth into a cliff, so the limit belongs on the
context. Skill 13 (cost and token economics) and skill 14 (reliability) in the [goal](../GOAL.md).
