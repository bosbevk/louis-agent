# FX8 · A FIX-RESULT with quotes in its summary is still read

> **Status:** done (2026-10-11) · **Area:** orchestrator, `LouisAgentClient.ParseFixResult` · **Found:** the full demo
> rerun for F14 (2026-10-11, `run-20261011-083646`) · **Commit:** (built in F14; see its plan)

## Symptom

Error 10 of the full demo (`customer-initials 1012`, the customer "Cher") was fixed, but the comms log said
`no result (4 msg, 24 tools)`, with no branch or commit, and the run summary listed 9 branches ready to merge, not 10.
louis-agent had ended every one of its four replies with the same line:

```
FIX-RESULT: {"status":"fixed","commit":"13eb2b0","branch":"fix/customer-initials-9c8637c0128d","tests":"16/16 passed","summary":"Single-word names now return one initial …, so order 1012 ("Cher") no longer throws IndexOutOfRangeException."}
```

and the orchestrator answered each with "Continue the fix from where you stopped. Finish with the FIX-RESULT line."
The fix itself was there (branch `fix/customer-initials-9c8637c0128d`, commit `13eb2b0`), and the orchestrator's own
checks confirmed it. F14 showed what it cost: R1.70, against about R0.95 for a one-message fix.

## Cause

The summary quotes the customer's name with unescaped double quotes, `("Cher")`, so the line isn't valid JSON.
`ParseFixResult` parsed it with `JsonDocument` and returned null on a `JsonException`, which reads as "no FIX-RESULT",
and the fix loop asks again (up to three follow-ups). The model wrote the same line every time, so asking again could
never help.

## Fix

When the line isn't valid JSON, its fields are read from the text: the short ones (`status`, `commit`, `branch`,
`tests`) are quote-free values; the free-text ones (`summary`, `reason`) run to the next known field or the closing
brace, so quotes inside them are kept. A line without a `status` still gives nothing, as before.

The alternative, telling the model to escape quotes in the fix prompt, would make the mistake rarer but not impossible;
reading tolerantly makes it harmless. (The prompt can still be tightened later.)

## Tests

`ParseFixResult_UnescapedQuotesInTheSummary_StillReadsTheResult` (the real line from the run) and
`…InAReason_StillReadsTheFailure` in `LouisAgentClientTests`; the existing "no valid line" cases still return null.

## Confirmed

The tests above with the run's own line. The full sample (`samples/sample-output/`) is the run that found it, kept as
it was; the `-Short` demo after the fix is in `samples/sample-output-short/`.

## Lesson

A machine-readable line written by a model is still model output: parse it tolerantly, and never answer a parse
failure with a request the model can't act on differently, since every retry is paid for (skill map #5, structured
outputs, which F8 addresses with results as tool calls; #13, cost).

---
[Fixes](README.md) · Previous: [FX7](FX07-timeout-ends-run.md)