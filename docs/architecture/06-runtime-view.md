# 6. Runtime view

> **Explanation.** How the parts work together at run time: one chat turn, the guards around tool calls, and the
> scenarios that span several agents. **Elsewhere:** a turn step by step, with what each host shows, in
> [how a turn works](../AGENT_INTERACTION.md); every message on the wire in
> [AGENT_COMMUNICATION.md](../AGENT_COMMUNICATION.md).

## A chat turn

```
User message
  ↓ host (CLI / ACP / API) → AgentEngine.StreamPromptAsync(history, message)
  ↓ system prompt = skill documentation; ChatOptions: tools, reasoning, 16k output limit
  ↓ FunctionInvokingChatClient loop
  │   model streams thinking + text, then tool calls
  │   each call → ToolCallGuards.InvokeAsync (log, run, guard the result) → result back to the model
  ↓ answer streams to the host as it is written
  ↓ the whole turn (thinking blocks, calls, results, answer) is appended to the history
```

### Guards around tool calls
- **Oversized results:** a result over 50,000 characters is summarised by a separate, tool-less model call (its input
  capped at 200,000 characters), falling back to truncation, so one tool can't flood the context.
- **Cut-off calls:** if a reply hits the output limit, any tool call in it has incomplete arguments. It is not run; the
  model is told to write large content in chunks. When streaming, a cut-off call is dropped by the provider, so the
  engine asks the model to continue (up to 3 times).
- **Failures:** exceptions reach the model with their message (e.g. a missing parameter) and are logged as failures.
- **A sub-session's prompt budget** (FX14): in a sub-session, once the next request is expected to pass
  `SUB_SESSION_PROMPT_BUDGET` (default 80,000 tokens, under Haiku 5.5's long-prompt price step), it keeps its tools with
  tool mode None, so the specialist answers from what it has instead of growing its context further.

## Scenarios across agents

| Scenario | What happens | In detail |
|---|---|---|
| **Puls delegates** | Puls opens a sub-session on its specialist with only the toolsets the task needs and a role; the specialist's steps stream into Puls's Werk panel; Puls answers in its own voice | [Agent to agent: Puls and its specialists](../AGENT_COMMUNICATION.md#agent-to-agent-puls-and-its-specialists) |
| **The orchestrator fixes an error** | It reads the service's error, follows the method's runbook, opens a fix session on louis-agent.api, verifies the commit and replays the request | [Agent to agent: the orchestrator and louis-agent](../AGENT_COMMUNICATION.md#agent-to-agent-the-orchestrator-and-louis-agent) · [Runbooks](../RUNBOOKS.md) |
