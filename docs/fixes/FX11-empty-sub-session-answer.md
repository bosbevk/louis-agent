# FX11 · A sub-session that ends without an answer is asked once more, in the same session

> **Status:** done · **Area:** core, the `agents` toolset (`AgentPeerTools.StartSubSession`) · **Found:** 2026-10-13

## Symptom

In the [Puls delegation check with roles](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-roles/README.md), question numbers-2
("how many test methods are in core's tests?") cost $0.0166 and took 105 s. That is a third of the delegated run, on
one question. Puls said so in its answer: *"Die sub-sessie het net die gebruikslyn teruggestuur, nie die telling self
nie. Ek probeer dit weer…"* The first specialist's answer was empty (`[TOOL] <- StartSubSession: ` followed by only the
usage line), so Puls sent the whole task to a second, new specialist.

## Cause

Proved from the run's ledger (`delegated/usage.jsonl`, the specialist session `sess_f8f589d6…`). It has 11 requests:
ten ending in `tool_calls` and the eleventh ending in `stop` with 104 output tokens. That is the 10-round limit (known
limitation 6). After the tenth round, the tool loop sends one last request with `tool_choice: none` (FX1), so the
model has to answer in text. This one spent its 104 tokens on thinking and wrote no text. The peer streamed a reply
with usage and an empty answer, and `StartSubSession` passed that on as a normal answer.

The retry was expensive because it was a new sub-session. The second specialist started cold: it wrote its prefix to
the cache again, then found the folder, counted, and read files again. The first specialist had already done all of
that, and the results were in its history.

## Fix

When the answer is empty, `StartSubSession` sends one follow-up message in the **same** sub-session
(`ToolMessage.SubSessionAskForAnswer`): *answer the task now from what you found, and say what you couldn't finish*.
The orchestrator already continues a fix this way ("Continue the fix…"). A new message starts a new turn on the peer,
with its own ten rounds and the history kept, so the specialist answers from what it already found, from a cached
prefix. The answer the model gets carries both turns' usage (`… + …`).

If the follow-up is empty too, the tool returns `SubSessionNoAnswer`: an error telling Puls not to send the same task
again, and to split the task or tell the user instead. There's one follow-up and no loop, and both turns share the
sub-session's 5-minute limit.

Considered and not done:
- **Raising the round limit for specialists.** That is F10's job (route settings). Any limit can be reached, so the
  empty ending needs handling anyway.
- **Returning the empty answer as an error and letting Puls decide.** Puls's only move is a new sub-session, which is
  what cost the money here.
- **Fixing it on the peer**, by having the engine ask again itself. The orchestrator's continue logic sits with the
  caller too. Puls's live view also shows the follow-up as part of the same delegation, which is honest about what
  happened.

## Tests

In `AgentPeerToolsTests`:
- `AnEmptyAnswer_IsAskedForOnce_InTheSameSession`: an empty reply, then an answer. Checks two messages to the same
  session, the follow-up's text, the answer, both usage lines, and one delete.
- `EmptyTwice_SaysSo_AndDoesNotAskAgain`: two empty replies. Checks for `SubSessionNoAnswer`, exactly two messages, and
  that the session is deleted.

`ToolMessageSnapshot.json` gained the two texts.

## Confirmed

2026-10-14, run 3 of the [three-run delegation check](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-repeat/README.md). The numbers-2 analyst used all ten tool rounds
and ended its turn without text (`stop`, 170 output tokens). The follow-up in the same session (turn 2) gave the
answer, and Puls called `StartSubSession` once. The earlier [check after FX10–FX12](https://github.com/bosbevk/louis-agent/blob/pre-cl10/samples/sample-output-puls-check-fixes/README.md)
had no empty answer. If a specialist hits its round limit again,
its sub-session should show two messages and one answer, and Puls should not start a second sub-session for the same
task.

## Lesson

Continue the context you already paid for; don't start over. A retry in a fresh session re-buys the prefix and redoes
the work. A follow-up in the same session reads both from the cache. The same idea is behind prompt caching (F4) and
the orchestrator's continue loop. Skill 10 (multi-agent orchestration) and skill 13 (cost and token economics) in the
[goal](../GOAL.md).
