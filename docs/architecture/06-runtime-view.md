# 6. Runtime view

> **Explanation.** How the parts work together at run time: what the model can use (tools and skills), one chat turn
> step by step with its guards, what each host shows, and the scenarios that span several agents. **Elsewhere:** every
> message on the wire in [AGENT_COMMUNICATION.md](../AGENT_COMMUNICATION.md); adding
> [a C# tool](../../louis-agent.core/CLAUDE.md#adding-a-c-tool), [a toolset](../../louis-agent.core/CLAUDE.md#adding-a-toolset),
> [a skill](../CLAUDE.md#adding-a-skill), [a mode or a role](../CLAUDE.md#adding-a-mode-or-a-role) or
> [another agent on core](../../louis-agent.core/CLAUDE.md#building-another-agent-on-core) in the development guides.

## Two kinds of capability

| | **Tools** | **Skills** |
|---|---|---|
| What | Public C# methods on the tool classes in `louis-agent.core/Tools/{Toolset}/` | Markdown in `louis-agent.core/Prompts/` |
| The model sees | A function with typed parameters and a description | Text in the system prompt; named procedures it can run |
| How it's used | The model calls it; the result comes back | The model follows the guidance, or runs a procedure with `ExecuteSkill` |
| Examples | `ReadWorkspaceFile`, `GetStatus`, `DotNetTest`, `WebSearch`, `LogTimeByTaskName` | `agent/louis-agent/voice.md` (the coding agent's mode: voice, routing), `toolsets/dotnet.md`, `agent/functions/code-review.md` |

Tools give the agent abilities; skills tell it how and when to use them. The agent can add both: `CreateSkillFile` writes
a new skill file and `CreateTool` a script-backed tool, and each waits in a `pending/` folder for your `/approve <Name>`
before any later session loads it (FX20 for skill files).

### How tools are registered

A session names its toolsets when it opens and keeps them (F5). Each toolset is an entry in `Agent/Toolsets/ToolsetCatalog.cs`
that builds its tool objects, and the engine registers only those:

```csharp
// AgentEngineFactory: the session's toolset names → their tool objects
var resolved = ToolsetCatalog.Resolve(toolsets, Context);                  // e.g. "files", "git"
var engine = new AgentEngine(skills, chatClient, options, supportsTools,
    toolsets: ToolsetCatalog.BuildTools(resolved, Context), ...);

// AgentEngine constructor (simplified)
_registry.Add(Function(_skillTools.ExecuteSkill, nameof(ExecuteSkill)));   // every session
_registry.AddPublicMethods(new ClockTools());                             // every session: today's date (F22)
foreach (object toolset in toolsets) _registry.AddPublicMethods(toolset); // only the session's toolsets
if (selfExtension) AddSelfExtensionTools();                               // the skills toolset: CreateTool, CreateSkillFile…
```

Every **public** instance method becomes a tool, named after the method, with its `[Description]` attributes as the
documentation. Keep helpers `internal` or `private`. `ToolRegistry` keeps every tool in name order, so the start of
every request is the same bytes and the prompt cache holds (F4). A toolset whose settings are missing (Paymo or DevOps
without its API key) can't be loaded. An agent built on core, like the orchestrator, passes its own tool objects as
`toolsets:` the same way (the guide's [Building another agent on core](../../louis-agent.core/CLAUDE.md#building-another-agent-on-core)).

### How skills are loaded

`AgentHost.LoadSkills` combines, in this order:
1. **the principles** (`shared/principles.md`), the same for every agent (ADR 0020);
2. **the role**: at level 0 the session's mode, its voice then its routing (ADR 0019: the default
   `louis-agent/voice`, or Puls's language file then `puls/voice.md`); at level 1 a specialist's role from
   `specialists/`, with no routing, since its toolsets were chosen for it;
3. **the guide of each toolset the session loaded** (`toolsets/{toolset}.md`) and no other (F5);
4. **with the `skills` toolset only**, every function skill the agent wrote.

The combined text is the system prompt. The built-in files are in `louis-agent.core/Prompts/` and read only from the
assembly (ADR 0018), so a wording change is a rebuild.

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

## A turn, step by step

**1. You send a message** — from Rider, the web app, the API or the CLI. Attached files are added to the message as
fenced code blocks (`[Attached file: name]`).

**2. The host streams the turn**

```csharp
await foreach (var update in engine.StreamPromptAsync(history, message, cancellationToken))
{
    // update.Contents: TextReasoningContent (thinking), TextContent (answer),
    // FunctionCallContent (a tool call), FunctionResultContent (its result)
}
```

`StreamPromptAsync` adds your message to the history and calls the model with the tools, `ChatOptions.Reasoning` (from
`LLM_THINKING`) and a 16,000-token output limit.

**3. The model thinks and decides.** With thinking on, it first reasons (streamed to you), then either answers or calls
one or more tools.

**4. Tools run.** `FunctionInvokingChatClient` runs each call through `ToolCallGuards.InvokeAsync`, which logs it,
runs it, and guards the result:
- a result over 50,000 characters is summarised by a separate model call instead of filling the context;
- a call cut off by the output limit is not run, and the model is told to write large content in chunks;
- an exception goes back to the model with its message, so it can correct itself.

The result is added to the conversation and the model continues — up to 10 tool rounds per message.

**5. The answer streams back** as it is written. When the stream ends, the whole turn — thinking (with its signature),
tool calls, results and answer — is in the history, so follow-up messages have full context.

### What each host shows

| | Thinking | Answer | Tool call |
|---|---|---|---|
| Rider (ACP) | `agent_thought_chunk` | `agent_message_chunk` | `tool_call` → `tool_call_update` (completed / failed) |
| HTTP API / web app | `thinking_delta` block | `text_delta` block | `tool_use` block, then `tool_result` block |
| CLI | grey text | text | `  > Name args` then `    done:` / `failed:` |

## Example

```
You: What branch am I on and what's the latest commit?

[thinking] The user wants the branch and latest commit. I'll use GetCurrentBranch and GetLog with count 1.
[tool]     GetCurrentBranch()          → main
[tool]     GetLog(count: 1)            → 306a91c docs: add a README and fix the Rider MCP server config
[answer]   You're on **main**; the latest commit is `306a91c` — "docs: add a README and fix the Rider MCP server config".
```

The model can call several tools in one round (here both lookups at once) and chain rounds when a result decides the
next step (read a file → edit it → build → run the tests).

## Writing large files

A reply has a 16,000-token output limit, and a whole web page or long source file can exceed it in one tool call. The
files guide (`toolsets/files.md`) tells it to write large files in chunks: `WriteWorkspaceFile` for the first ~300
lines, then `AppendToFile` for each next chunk. If a call is still cut off, it is skipped and the model is asked to
continue in smaller pieces (up to 3 times when streaming).

## Debugging

Every call is logged to stderr and, in Docker, to `logs/agent-{host}-{time}.log`:

```
[TOOL] -> GetLog({"count":1})
[TOOL] <- GetLog: 306a91c docs: add a README and fix the Rider MCP server config
[TOOL] <- ReadWorkspaceFile failed: ArgumentException: Path must stay inside the workspace. (Parameter 'relativePath')
[WARN] Reply cut off by the output limit (16000 tokens); asking the model to continue
```

`logs/oversized-tool-results.jsonl` and `logs/truncated-tool-calls.jsonl` record the cases where a tool returned too
much or a reply ran out of room.

To see what the model is offered, inspect `engine.Tools` (name, description, JSON schema) in a test or the debugger.

## Scenarios across agents

| Scenario | What happens | In detail |
|---|---|---|
| **Puls delegates** | Puls opens a sub-session on its specialist with only the toolsets the task needs and a role; the specialist's steps stream into Puls's Werk panel; Puls answers in its own voice | [Agent to agent: Puls and its specialists](../AGENT_COMMUNICATION.md#agent-to-agent-puls-and-its-specialists) |
| **The orchestrator fixes an error** | It reads the service's error, follows the method's runbook, opens a fix session on louis-agent.api, verifies the commit and replays the request | [Agent to agent: the orchestrator and louis-agent](../AGENT_COMMUNICATION.md#agent-to-agent-the-orchestrator-and-louis-agent) · [Runbooks](../projects/orchestrator-runbooks.md) |
