using LouisAgent.Composition.Tools;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages.Holes;
using System.ComponentModel;
using LouisAgent.Core.Agent.Messages.Holes.AgentBuilt;
using LouisAgent.Core.Agent.Messages.Holes.Calls;
using LouisAgent.Core.Agent.Messages.Holes.Discovery;
using LouisAgent.Core.Agent.Messages.Holes.Summaries;
using LouisAgent.Core.Agent.Messages.Holes.Toolsets;

namespace LouisAgent.Core.Agent.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger), and its
// holes record names them. Members are added as code is written or moved.

/// <summary>Log lines for the agent's own turns and tools, written to the console and the run file.</summary>
public enum AgentLogMessage
{
    [Description("-> {Tool}({Arguments})")]
    [Holes(typeof(ToolCallHoles))]
    ToolCall,

    [Description("<- {Tool}: skipped, call was cut off by the output limit ({MaxOutputTokens} tokens)")]
    [Holes(typeof(ToolCallSkippedHoles))]
    ToolCallSkipped,

    [Description("<- {Tool} failed: {ErrorType}: {Reason}")]
    [Holes(typeof(ToolCallFailedHoles))]
    ToolCallFailed,

    [Description("<- {Tool}: {Result}")]
    [Holes(typeof(ToolCallResultHoles))]
    ToolCallResult,

    [Description("summarising {Tool} output ({Chars} chars)")]
    [Holes(typeof(ToolResultSummarisingHoles))]
    ToolResultSummarising,

    [Description("Could not summarise {Tool} output: {Reason}")]
    [Holes(typeof(SummaryFailedHoles))]
    SummaryFailed,

    [Description("Model does not support tools; running without them.")]
    ModelWithoutTools,

    [Description("Cannot register tool '{Name}': model does not support tools")]
    [Holes(typeof(McpCannotRegisterHoles))]
    McpCannotRegister,

    [Description("Reply cut off by the output limit ({MaxOutputTokens} tokens); asking the model to continue")]
    [Holes(typeof(ReplyCutOffHoles))]
    ReplyCutOff,

    [Description("Tool '{Name}' already registered; skipping")]
    [Holes(typeof(McpToolAlreadyRegisteredHoles))]
    McpToolAlreadyRegistered,

    [Description("Discovered tool from Rider: {Name} - {Description}")]
    [Holes(typeof(McpToolDiscoveredHoles))]
    McpToolDiscovered,

    [Description("Skipping invalid tool {File}: {Errors}")]
    [Holes(typeof(AgentToolInvalidHoles))]
    AgentToolInvalid,

    [Description("Skipping tool {File}: name '{Name}' is already in use.")]
    [Holes(typeof(AgentToolNameInUseHoles))]
    AgentToolNameInUse,

    [Description("Loaded {Count} agent-built tool(s) from {Directory}")]
    [Holes(typeof(AgentToolsLoadedHoles))]
    AgentToolsLoaded,

    [Description(
        "Sub-session prompt budget: the last request's prompt was {Last} tokens and the next is expected at {Next}, " +
        "over the budget of {Budget}; the next request answers without tools")]
    [Holes(typeof(SubSessionPromptBudgetReachedHoles))]
    SubSessionPromptBudgetReached,

    [Description("Optional toolset '{Name}' left out: this instance needs {Settings} for it")]
    [Holes(typeof(OptionalToolsetSkippedHoles))]
    OptionalToolsetSkipped,
}
