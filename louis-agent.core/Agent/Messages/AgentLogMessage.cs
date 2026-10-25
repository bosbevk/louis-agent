namespace LouisAgent.Core.Agent.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the agent's own turns and tools, written to the console and the run file.</summary>
public enum AgentLogMessage
{
    [Description("-> {Tool}({Arguments})")]
    ToolCall,

    [Description("<- {Tool}: skipped, call was cut off by the output limit ({MaxOutputTokens} tokens)")]
    ToolCallSkipped,

    [Description("<- {Tool} failed: {ErrorType}: {Reason}")]
    ToolCallFailed,

    [Description("<- {Tool}: {Result}")]
    ToolCallResult,

    [Description("summarising {Tool} output ({Chars} chars)")]
    ToolResultSummarising,

    [Description("Could not summarise {Tool} output: {Reason}")]
    SummaryFailed,

    [Description("Model does not support tools; running without them.")]
    ModelWithoutTools,

    [Description("Cannot register tool '{Name}': model does not support tools")]
    McpCannotRegister,

    [Description("Reply cut off by the output limit ({MaxOutputTokens} tokens); asking the model to continue")]
    ReplyCutOff,

    [Description("Tool '{Name}' already registered; skipping")]
    McpToolAlreadyRegistered,

    [Description("Discovered tool from Rider: {Name} - {Description}")]
    McpToolDiscovered,

    [Description("Skipping invalid tool {File}: {Errors}")]
    AgentToolInvalid,

    [Description("Skipping tool {File}: name '{Name}' is already in use.")]
    AgentToolNameInUse,

    [Description("Loaded {Count} agent-built tool(s) from {Directory}")]
    AgentToolsLoaded,

    [Description("Sub-session prompt budget: the last request's prompt was {Last} tokens and the next is expected at {Next}, over the budget of {Budget}; the next request answers without tools")]
    SubSessionPromptBudgetReached,

    [Description("Optional toolset '{Name}' left out: this instance needs {Settings} for it")]
    OptionalToolsetSkipped,
}
