# FX1 · Keep the tools on the request after the 10-round limit

> **Status:** done (2026-10-10) · **Area:** core, the tool loop (`AgentEngine`) · **Commit:** `d9c7c45`
>
> **Removed the limitation:** "On Claude Haiku 5.5 a message that uses all 10 tool rounds fails with a 400"

## Symptom

The first short demo on Claude Haiku 5.5 (`run-demo.ps1 -Short`, run `run-20261010-165434`) confirmed only **2 of 4**
fixes. For the other two, louis-agent's turn ended in an error and the orchestrator escalated them, although one fix had
already been committed. louis-agent.api's log:

```text
[API] Turn failed in sess_25ff…: Anthropic.Exceptions.AnthropicBadRequestException: Status Code: BadRequest
invalid_request_error: messages.1.content.0: Invalid `signature` in `thinking` block. The block is bound to a different
conversation. … The `tools` list differs from the one this block was created with.
```

The same code had fixed all ten bugs on Claude Haiku 4.5.

## Cause

- **The usage ledger pointed at it.** Both failed sessions had exactly **10 requests, the last ending in `tool_calls`**:
  they had used all 10 tool rounds of the message (`MaximumIterationsPerRequest = 10`) and still wanted more. The two
  fixes that worked had 9 and 10 requests ending in `stop`.
- **A test measured what the tool loop sends next.** A fake model that never stops calling tools showed requests 1–10
  with all 73 tools in `Auto` mode, and an 11th with **`tools = null, tool mode = null`**: `FunctionInvokingChatClient`
  drops the tools at the limit to force a text answer.
- **Haiku 5.5 binds thinking blocks to the conversation**, tool list included (its migration guide: "keep earlier turns
  unchanged"). The 11th request sends the earlier thinking blocks with a different tool list, so it is rejected. Haiku
  4.5 doesn't check, so there the same request just returned text without a `FIX-RESULT`, and the orchestrator asked
  louis-agent to continue.

## Fix

A middleware inside the tool loop, `AgentEngine.KeepToolsOnFinalRequest`: a request that arrives with no tools while
the engine has some is the loop's final one (every turn's requests carry the tools), so it gets the engine's tools back,
unchanged, with **`ChatToolMode.None`**. The tool list then matches every earlier request, and the model still can't call
a tool. Captured locally (no real call), the Anthropic adapter sends that as the same `tools` with
`tool_choice: {"type": "none"}`. Models without tool support, and every request that already has tools, are untouched.

**Rejected:** raising the round limit (that's [F10](../features/F10-route-settings.md), and only makes the limit rarer, not
safe); turning thinking off for the final request (Haiku 5.5's thinking can't be turned off).

## Tests

`tests/louis-agent.core.tests/Tools/AgentEngineRoundLimitTests.cs`: past the limit the final request keeps the same tool
list with mode `None` (streaming and not); the 10 rounds before it are unchanged; a model without tools is left alone;
the caller's options aren't modified.

## Confirmed

- Short demo rerun (`run-20261010-171318`): **4/4 fixes**, no failed turn. One fix reported on its 11th request; another
  hit the limit, ended cleanly, and finished in a second message.
- Full demo (`run-20261010-193730`): **10/10 fixes**; four needed a second message after using all 10 rounds.

## Lesson

**Preserved thinking**: newer Claude models tie their thinking blocks to the exact conversation (system prompt, tools,
earlier messages), so any layer that edits a request in flight, even a library's, must keep it append-only. Skill map #2
(reasoning and extended thinking) and #14 (reliability). The same rule is behind [FX4](FX04-append-only-history.md).

---
[Fixes](README.md) · Next: [FX2](FX02-probe-ollama-404.md)