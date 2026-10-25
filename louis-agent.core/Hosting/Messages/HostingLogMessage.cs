namespace LouisAgent.Core.Hosting.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the host's start-up and sessions, written to the console and the run file.</summary>
public enum HostingLogMessage
{
    [Description("LLM_PROFILE '{Profile}' has no {File}; using the LLM settings from .env and the defaults.")]
    LlmProfileMissing,

    [Description("Display currency: {Display}")]
    DisplayCurrencyChosen,

    [Description("LLM: {Llm}; tools={Tools}; thinking={Thinking}")]
    LlmChosen,

    [Description("A session may also name LLM profile '{Name}': {Llm}; tools={Tools}; thinking={Thinking}")]
    SessionLlmLoaded,

    [Description("Starting MCP tool discovery from {Endpoint}")]
    McpDiscoveryStarting,

    [Description("Startup task exception: {Error}")]
    McpStartupFailed,

    [Description("MCP discovery disabled or not configured")]
    McpDiscoveryDisabled,

    [Description("Usage ledger off: LOG_DIRECTORY is not set. Set it to record token usage, or USAGE_LEDGER=off to silence this.")]
    LedgerOff,

    [Description("Usage ledger: {Path}")]
    LedgerAt,

    [Description("Loaded {Provider}")]
    SkillProviderLoaded,

    [Description("Loaded {Provider} (built in)")]
    SkillFileEmbedded,

    [Description("Toolsets {Toolsets}: {Tools} tools, a {PromptChars}-character system prompt, about {PrefixTokens} tokens of fixed prefix")]
    ToolsetProfileBuilt,

    [Description("Toolsets {Toolsets}: dropped from the cache, the least recently used of {Capacity} profiles")]
    ToolsetProfileDropped,

    [Description("Discovered skills file: {File}")]
    SkillsFileDiscovered,

    [Description("No tools discovered from Rider MCP server")]
    McpNoToolsDiscovered,

    [Description("Failed to register tool '{Name}': {Reason}")]
    McpRegisterFailed,

    [Description("Discovered and registered {Registered}/{Count} tools from Rider")]
    McpRegistered,

    [Description("Tool discovery failed: {Reason}")]
    McpDiscoveryFailed,
}
