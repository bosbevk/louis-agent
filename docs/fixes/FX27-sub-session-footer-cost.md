# FX27 · A sub-session's answer carries no usage, so a replay matches its recording

> **Status:** open · **Area:** core: the sub-session tools' answers (`SpecialistTools`, `MinionTools`,
> `SubSessionMessenger`, `SubSessionToolMessage`) · **Found:** 2026-10-29, while finishing FX26: the cause behind its
> replay misses · **Branch:** `fix/FX27-sub-session-footer-cost`, from FX26's tip (Louis's pick: FX26 is not on main
> yet, so FX27 merges after it) · **Plan:** [FX27](../plans/FX27-sub-session-footer-cost.md)

## Symptom

The free replay of the cook baseline (`6edc15cc1d1e`, and again after FX26 as `176ad599149e`, both of
`5d1fd231c9b1`) misses at 8 of its 20 turns, all in the cook mode, the one that delegates:

```
No recorded model answer for this request in trial '5d1fd231c9b1:puls-cook:1/cook-af':
message 3 (tool) changed. The model was not called.
```

That's every delegating first turn (six tasks), and then, on the two follow-up tasks, `cook-af` and `cook-en`, the
second turn too, with `message 2 (user) was added`. Since FX26 those turns count as infrastructure instead of as the
agent's failure, but they still have no answer: the replay can't grade the cook's delegating work at all.

## Cause

A specialist's answer comes back to the parent as a tool result with a footer, and the footer holds what the
specialist used. The same task, recorded and replayed:

```
[Sub-session sub-1 on puls (web; puls-cook): 5 requests, 104,249 tokens, 0.0098 USD. It stays open: …]
[Sub-session sub-1 on puls (web; puls-cook): 5 requests, 0 tokens, 0 USD. It stays open: …]
```

On a replay every model answer comes from the recording and is a known 0 in the ledger, which is right. But the
footer is text the parent reads: it is message 3 of the parent's next request, and a replay finds its answer by a hash
of everything the model reads. Different numbers, different hash, no recorded answer. The footer's usage is built by
`SubSessionUsage.Line()` into `SubSessionToolMessage.SubSessionAnswer`, `SubSessionAnswerKept`, `MinionAnswer` and
`SubSessionNoAnswer`, so a minion's answer has the same flaw.

The second turn's miss follows from the first: the first turn failed, so its answer never joined the history, and the
second turn's request (the first question, then the second) matches no recording that had the answer in between.

The footer's usage also goes against our own rule: the ledger is where cost lives
([ADR 0007](../adr/0007-measure-every-request.md)), and the model gains nothing from it. The prompts already say so:
the usage line is "already in the usage ledger, linked to this turn", and its cost is "never in your answer. The chat
shows the cost already" (`toolsets/specialists.md`, `toolsets/minions.md`). The usage itself is rolled up into the
parent's turn and the ledger (`UsageRollUp.Record`, one sub-session row per reply), so taking it out of the prompt
loses nothing.

## Options

Three were put to Louis (2026-10-29):

1. **Take the usage out of the footer** (picked): remove what changes between runs from what the model reads, as the
   replay's key already leaves out a message's time and id. The footer keeps what the model needs to find the
   sub-session again: its handle, agent, toolsets and role. Costs: model-visible text changes (the goldens), and the
   old recordings no longer match, so the cook baseline is recorded again
   ([ADR 0049](../adr/0049-nothing-is-compatible-before-a-sale.md)).
2. **Mask the usage in the replay's key:** the key hashes a tool result with its footer's numbers blanked. The model
   would still see the cost, but the replay's key code would have to know one tool's wording, and break silently
   when it changes; the key would no longer be what the model read; and the recordings would still need redoing.
3. **Replay the usage too:** on a hit the child's stream reports the recorded usage while the ledger writes 0. No
   re-recording, but two usage figures per request through the API, the most code, and a new way for the views and
   the ledger to disagree.

Option 1 because it fixes the cause rather than hiding it from the key, follows ADR 0007, and is the smallest change.

## Fix

1. **The usage leaves every tool result text.** The footers keep what the model needs to find the child again:
   `[Sub-session on coder (files, git)]`, `[Sub-session sub-1 on puls (web; puls-cook). It stays open: …]`,
   `[Minion {role} on {llm} with {tool}]`, and `SubSessionNoAnswer` without its usage. `SubSessionUsage.Line()` and
   the `SubSessionUsage`, `SubSessionNoUsage` and `SubSessionCostUnknown` texts are gone, and
   `SubSessionMessenger.SubSessionReply` carries no usage line; the usage still goes to the parent's turn and the
   ledger (`UsageRollUp`). The `StartSubSession` description says "Returns the answer". The chat's cards already took
   a child's cost from its own events (`Block.SubTurn.Usage`, from its `message_delta`); their fallback to the footer
   is gone, and `Delegation`'s footer patterns lost their usage part (and the minion's handle part, which never
   matched: minions are never kept). The guides (`specialists.md`, `minions.md`) and Puls's routing no longer
   promise a usage line: the cost is in the ledger and the chat, never in the answer. The recorded stream fixture's
   one footer was edited to the new shape. While the work panel's markup was in the step, its nested ternaries became
   `StepMarks`.

## Tests

`SpecialistToolsTests.TheAnswer_ReadsTheSame_WhateverTheChildUsed` (kept and not) and
`MinionToolsTests.TheAnswer_ReadsTheSame_WhateverTheMinionUsed`, written first: the same reply streamed with real
usage and with zero usage (as a replay reports it) must give the same tool result text. Red on the footer, e.g.
`Expected: "...(web): 3 requests, 10,200 tokens, 0.0123 USD]" But was: "...(web): 3 requests, 0 tokens, 0 USD]"`;
green after. `DelegationTests` now reads the new footers, and the recorded turn's cost comes from the cook's own
events. The goldens (`ToolMessageSnapshot.json`, `ModelRequestSnapshot.json`, `PulsSessionSnapshot.json`) changed
only in the footers, the tool description and the reworded guide lines.

## Confirmed

(To come. The old baseline can't confirm it: its recording holds the footer with the numbers, so after the fix a
replay of it misses the other way. Step 1's test proves the parent's request is the same whatever the child used; then
one fresh recorded cook run, a paid run that waits for Louis's go, and a free replay of it.)

## Lesson

(To come.)
