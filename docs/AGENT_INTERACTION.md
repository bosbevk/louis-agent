# Louis Agent: How a Turn Works

How the agent turns your message into tool calls and an answer. For the components themselves see
[ARCHITECTURE.md](ARCHITECTURE.md).

## Two kinds of capability

| | **Tools** | **Skills** |
|---|---|---|
| What | Public C# methods on the tool classes in `louis-agent.core/tools/` | Markdown in `Skills/*.md` |
| The model sees | A function with typed parameters and a description | Text in the system prompt; named procedures it can run |
| How it's used | The model calls it; the result comes back | The model follows the guidance, or runs a procedure with `ExecuteSkill` |
| Examples | `ReadWorkspaceFile`, `GetStatus`, `DotNetTest`, `WebSearch`, `LogTimeByTaskName` | `default.md` (behaviour, tool overview), `dotnet-skills.md`, `code-review-skills.md` |

Tools give the agent abilities; skills tell it how and when to use them. The agent can add both: `CreateSkillFile` writes
a new skill file, and `CreateTool` writes a script-backed tool that waits for your `/approve <Name>`.

### How tools are registered

```csharp
// AgentEngine constructor (simplified)
_tools.Add(AIFunctionFactory.Create(ExecuteSkill, nameof(ExecuteSkill)));
AddPublicMethodsAsTools(new WorkspaceTools(WorkspaceRoot));
AddPublicMethodsAsTools(new GitTools(WorkspaceRoot));
AddPublicMethodsAsTools(new DotNetTools(WorkspaceRoot));
// ... Python, PowerShell, Bash, Web; Paymo and DevOps only when their keys are set
```

Every **public** instance method becomes a tool, named after the method, with its `[Description]` attributes as the
documentation. Keep helpers `internal` or `private`.

### How skills are loaded

`AgentHost.LoadSkills` combines `default.md`, the always-loaded guides (`dotnet`, `python`, `powershell`, `bash`, `web`,
`self-extension`) and the `AGENT_FUNCTION` file (`louis` loads them all). The combined text is the system prompt.

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

**4. Tools run.** `FunctionInvokingChatClient` runs each call through `AgentEngine.LogAndInvokeAsync`, which logs it,
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
agent's instructions (`default.md`) tell it to write large files in chunks: `WriteWorkspaceFile` for the first ~300
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

## Extending

### A new C# tool

```csharp
namespace louis_agent.core.tools;

using System.ComponentModel;

public sealed class MyTools
{
    [Description("What this tool does, in one sentence the model can act on")]
    public string MyTool([Description("What this parameter is")] string input) => "result";
}
```

Register it in the `AgentEngine` constructor with `AddPublicMethodsAsTools(new MyTools())`, mention it in the relevant
skill file, and add tests in `tests/louis-agent.core.tests/Tools/`. Tool names must stay unique — the Anthropic API
rejects duplicates.

### A new skill file

Drop `Skills/{name}-skills.md` in the folder. With `AGENT_FUNCTION=louis` it is picked up automatically; otherwise set
`AGENT_FUNCTION={name}`. Procedures inside it look like:

````markdown
## Skill: MySkill

- Description: What it does.
- Parameters:
  - `name` (string): What it is.
- Execution:

```bash
echo "Hello, ${SKILL_ARG_name}"
```
````

Arguments arrive as `SKILL_ARG_*` environment variables; quote them and never build commands from them.
