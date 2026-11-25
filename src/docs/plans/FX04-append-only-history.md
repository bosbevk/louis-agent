# FX4 Append-only history

> **Plan.** How a chat stops editing what it already sent, so its earlier thinking stays valid on every request, after
> a tool is made mid-message, a skill is reloaded, another chat changes the tools, or a release changes them before a
> resume. **Elsewhere:** the fix's record in [FX4](../fixes/FX04-append-only-history.md), the rule in
> [ADR 0008](../adr/0008-history-is-append-only.md).

![FX4 append-only history: a Conversation fixed at its first turn, changes reaching the next chat, a resume sending what it saved, and stale thinking handled per provider](diagrams/FX04-append-only-history-overview.svg)

## Context

Claude binds its thinking blocks to everything sent before them. In Anthropic's words, a thinking block is tied to
"`system`, `tools`, and every earlier message. If any of them changes, the block fails the prefix check, and the request
can get a 400 error. So only add to a conversation"
([preserved thinking](https://platform.claude.com/docs/en/build-with-claude/preserved-thinking), read 2026-11-25).
Both live sites run Claude Haiku 5.5 with thinking on (the default, `medium`), so this bites live.

Today's code still edits in five places, the ones FX4's record names, all checked in the code on 2026-11-25.
`TurnRunner.Begin` hands every request the engine's live tool list (`chatOptions.Tools = registry.Live`), so a tool
`CreateTool` makes mid-message changes the next request of the same message. `/approve` and `/reject` change a tool's
description or remove it. `Begin` rewrites `history[0]` when the skills were reloaded. The round-limit guard
(`ToolCallGuards.KeepToolsOnFinalRequest`) restores `registry.Live` on the final request. And engines are shared by
every chat with the same toolsets, so one chat's new tool changes the others'.

F43.1 added a sixth: a resumed chat gets a freshly built system prompt and today's tools, so a release that changes a
prompt or any tool breaks every resumed chat with thinking in it.

Louis, 2026-11-25: "FX4", then on the picks below. What was read: the code above; Anthropic's
[mid-conversation system messages](https://platform.claude.com/docs/en/build-with-claude/mid-conversation-system-messages),
[changing tools or instructions](https://platform.claude.com/docs/en/build-with-claude/preserved-thinking/change-tools-or-instructions)
and [prefix checks](https://platform.claude.com/docs/en/build-with-claude/preserved-thinking/prefix-checks);
OpenAI's [keep the prefix unchanged](https://developers.openai.com/api/docs/guides/keep-prefix-unchanged); Ollama's
[thinking](https://ollama.com/blog/thinking). Not confirmed: whether Ollama checks earlier thinking (its API page didn't
load), and whether the Anthropic adapter can send `thinking.block_binding` (step 9 proves it).

Louis's picks, 2026-11-25, on cards:

- **A resumed chat sends the system prompt it was saved with** ("Save the prompt"); a prompt change reaches new chats.
- **Mid-chat changes reach the next chat**, on every provider ("Freeze, then F97"); Anthropic's way of loading tools
  and skills mid-chat by appending is F97, after this.
- **A `Conversation` type** owns the history and its frozen tools, and every host passes it.
- **The round limit's final request finds the frozen list in a turn scope**, an `AsyncLocal`, as `UsageScope` does.
- **A resume after a release changed the tools is a provider strategy**: "our own will do 1", so the default strips
  the stale thinking itself, and Anthropic's asks the API to drop the blocks that fail (`drop_block`).

## Design

### The shape, in plain words

A chat becomes a `Conversation`: its messages, and the tool list it was frozen with. At its first turn the engine
copies its live tools into the conversation, and every request after, in every turn, sends that copy, never the live
list. The turn opens a `TurnTools` scope with the copy, so the round limit's final request puts back the same list.
The system prompt is written once, when the conversation starts, and never rewritten.

What changes the engine's tools or skills mid-chat still changes them: the engine's live list and its skills. They
reach the next conversation. Each tool that makes such a change says so in its reply, so the model doesn't promise to
use what it can't call yet.

A stored session keeps its system prompt and a fingerprint of its tools: each tool's name, description and schema,
hashed. A resume sends the saved prompt, and the tools as they are today. When today's fingerprint differs, the
provider's `IStaleThinking` strategy prepares the request: the default, ours, strips the thinking from the restored
turns; Anthropic's asks for `drop_block`, so only the blocks that really fail are dropped. OpenAI's API already drops
reasoning after a changed prefix by itself, so it takes the default.

### Interface design

![The interface design: AgentEngine starts a Conversation and runs turns on it; TurnRunner freezes its tools and opens TurnTools, which the guard reads; StoredSession restores a conversation; IStaleThinking with the default and Anthropic's strategy](diagrams/FX04-append-only-history-interface-design.svg)

`Conversation` is Fowler's *Encapsulate Collection*: the list a host used to hold bare is owned, with what belongs to
it. `TurnTools` is the *Ambient Context* the repo already uses for usage (`UsageScope`). `IStaleThinking` is a
*Strategy* chosen by the provider, as `IUsageMapper` and `IPromptCachingStrategy` are (ADR 0003).

### Architecture

![The architecture: the CLI, ACP and API pass a Conversation to the engine; the turn's tools scope and the guards in core; the engine tools' replies; the stale-thinking seam in core with Anthropic's strategy in its library; MongoDB keeps the prompt and fingerprint](diagrams/FX04-append-only-history-architecture.svg)

### Communication: a tool created mid-message

![A tool created mid-message: the first turn freezes the tools, CreateTool adds to the live list only, and every later request, the final one too, sends the frozen list](diagrams/FX04-append-only-history-communication.svg)

### Objects in memory: two chats on one engine

![Two conversations on one shared engine, each with its own frozen tools, and the turn scope pointing at the running one's](diagrams/FX04-append-only-history-objects.svg)

### Namespace map

| Type | Folder | Namespace | Step |
|---|---|---|---|
| `Conversation` (new) | `src/engine/LouisAgent.Core/Agent/` | `LouisAgent.Core.Agent` | 2 |
| `AgentEngine` (`StartConversation`, turns take a `Conversation`) | `Agent/` | `LouisAgent.Core.Agent` | 2 |
| `TurnRunner` (freezes, never rewrites the prompt) | `Agent/Turns/` | `LouisAgent.Core.Agent.Turns` | 3, 5 |
| `TurnTools` (new) | `Agent/Turns/` | `LouisAgent.Core.Agent.Turns` | 4 |
| `ToolCallGuards` | `Agent/Turns/` | `LouisAgent.Core.Agent.Turns` | 4 |
| The engine tools' and commands' texts | `Agent/Messages/`, `Agent/EngineTools/` | as now | 6 |
| `AgentSession`, `ConsoleChat`, `AcpSession` and the other hosts | each host | as now | 2 |
| `StoredSession`, `SessionDocument` (+ prompt, fingerprint) | `src/hosts/LouisAgent.Api/Sessions/Store/` | as now | 7 |
| `ToolsFingerprint` (new) | `Agent/` | `LouisAgent.Core.Agent` | 7 |
| `IStaleThinking`, `StripStaleThinking` (new) | `Llm/Thinking/` | `LouisAgent.Core.Llm.Thinking` | 8 |
| `ILlmProvider.CreateStaleThinking()` | `Llm/Providers/` | as now | 8 |
| `AnthropicDropBlock` (new) | `src/engine/LouisAgent.Core.Anthropic/` | `LouisAgent.Core.Anthropic` | 9 |
| Tests | mirroring each, in `LouisAgent.Core.Tests`, `LouisAgent.Api.Tests`, `LouisAgent.Core.Anthropic.Tests` | | 2 to 9 |

`Llm/Thinking/` is a new concern folder, so `ArchitectureTests`' layers and core's guide name it in step 8.

### What is reused

The provider seam (`ILlmProvider`, with a default member, as the usage mapper and the cache strategy are), the
`AsyncLocal` pattern of `UsageScope`, F43.1's store and its contract tests, and the golden files for the texts the
model reads. No new package.

## Steps

### Step 1: the plan is kept

A docs-only step: this plan, its five pictures as JSON and SVG, its row in the plans' index, and FX4's TODO row linking
it.

### Step 2: a Conversation type

A refactor with no change of behaviour, held by the tests already there, converted: `AgentEngine.StartConversation()`
replaces `StartHistory()`, and `ProcessPromptAsync` and `StreamPromptAsync` take a `Conversation`. Every host and test
that held a `List<ChatMessage>` holds a `Conversation`, its `Messages` where it read the list. Rider's signature change
does the callers. Run the projects that call the engine.

### Step 3: the tools are frozen at the first turn

The tests come first. A tool `CreateTool` makes mid-message: every later request of that message sends the same tool
list as the first. `/approve` and `/reject` between two messages leave the open conversation's tools, names and
descriptions, unchanged. A tool made in one conversation doesn't change another's on the same engine. Then `Begin`
copies the live list into the conversation on its first turn and sends that copy.

### Step 4: the final request keeps the conversation's tools

The test first, extending `AgentEngineRoundLimitTests`: a message that hits the round limit after the engine's tools
changed sends, on its final request, the conversation's frozen list with `tool_choice: none`. Then `TurnTools`, opened
by the turn, and the guard reading it instead of `registry.Live`.

### Step 5: the system prompt is written once

The test first: a skill reload between two messages leaves the conversation's first message as it was, and a new
conversation gets the new prompt. Then `Begin` stops rewriting it.

### Step 6: the replies say "your next chat"

The tests change first: `CreateTool`, `/approve`, `/reject`, `ReloadSkills` and an approved skill file say the change
reaches the next chat, not this one. Then their texts, and the golden files regenerated and read.

### Step 7: a stored session keeps its prompt and tools fingerprint

The tests first, in the API: a resume after the system prompt changed sends the saved prompt; the fingerprint is
saved, and a resume knows whether today's differs. The store's contract covers both fields on both stores. Then
`StoredSession` and `SessionDocument`, `ToolsFingerprint`, and the resume building its `Conversation` from them.

### Step 8: stale thinking, the default strategy

The tests first: on a resume whose fingerprint differs, the default strategy strips the thinking from the restored
turns, and keeps their text, tool calls and results; with the same fingerprint it changes nothing. Then
`IStaleThinking`, `StripStaleThinking`, `ILlmProvider.CreateStaleThinking()` defaulting to it, and core's layers.

### Step 9: Anthropic's drop_block

First a proof, a paid test call on Louis's word: a request through the Anthropic adapter with the beta header
`thinking-binding-controls-2026-08-01` and `thinking.block_binding.prefix_mismatch_behavior: "drop_block"`, after a
changed tool, answers instead of a 400. If the adapter can't send either, the plan stops here and the ways on go to
Louis, the default strategy covering Anthropic meanwhile. Then the test, offline, that Anthropic's strategy sets both
on a resume that moved, and `AnthropicDropBlock`.

### Step 10: the close

With `/fix-a-bug`'s close: FX4's record folded into the fixes table and its lesson, the TODO's known limitation
removed, the core and API design docs, the work log, and this plan deleted.

## Verification

Every step's tests are named in it. Before the pull request, `main` is merged in and the full unit suite runs. One paid
proof in step 9, on Louis's word.

**Done when:** on Haiku 5.5, on the local site: a message in which the model creates a tool and keeps working; then
`/approve`, a skill reload and a second open chat, each followed by another message; then a resume after a change to a
tool's description: no 400, in any of them.

## Concepts

| Step | What it teaches | Skill in [GOAL.md](../GOAL.md) |
|---|---|---|
| 2 | Owning a collection with what belongs to it, so state can't drift from its history | 9 Sessions and state |
| 3, 4, 5 | Preserved thinking: the prefix a model checks, and why only appending is safe | 2 Reasoning and extended thinking |
| 6 | Telling the model the truth about what it can call | 4 Tool design |
| 7, 8, 9 | A provider difference as a strategy, chosen once | 20 Extensibility |

## Approved during the build

**A tidy before step 2** (Louis, 2026-11-25, on a card: "Tidy first, own commit"). Rider's rename in step 2 reaches
four host files that still build anonymous objects (`AcpServer`, `TurnRelay`, `ConsoleChat`, the orchestrator's
`Program`), and fix-as-you-touch would fold their records into the refactor. So one commit before step 2 fixes them
whole, and step 2 stays the pure `Conversation` change.

**ACP's updates as our own records, a union** (Louis, 2026-11-25, on a card: "Our records, polymorphic", over the
unofficial `AgentClientProtocol` package and flat records). `session/update`'s `update` is an abstract
`SessionUpdate` record, `[JsonPolymorphic]` on `sessionUpdate`, one sealed record per kind the server sends, so the
wire's JSON stays as it is.

**The tidy in two commits** (Louis, 2026-11-25, on a card: "Two tidy commits"). The orchestrator's `Program` made
its log enum a touched file, so all 24 of its holed messages get records. The first tidy takes ACP and the CLI, the
second the orchestrator, each small enough to review on its own.

---
[Docs index](../README.md) · Related: [Plans](README.md) · [FX4](../fixes/FX04-append-only-history.md)
