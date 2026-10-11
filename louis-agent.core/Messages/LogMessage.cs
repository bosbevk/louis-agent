namespace LouisAgent.Core.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines ([INFO], [WARN]); written to stderr, never stdout.</summary>
public enum LogMessage
{
    [Description("{Tool} returned {Chars} characters, {Outcome} to {ReturnedChars}; arguments {Arguments}; request: {UserRequest}")]
    OversizedToolResult,

    [Description("{Tool} was cut off by the output limit ({MaxOutputTokens} tokens); arguments {Arguments}")]
    TruncatedToolCall,

    [Description("WORKSPACE_ROOT not set, using repository root: {Path}")]
    WorkspaceRootFromRepository,

    [Description("WORKSPACE_ROOT not set, using current directory: {Path}")]
    WorkspaceRootFromCurrentDirectory,

    [Description("Display currency: no {Code} rate (from {Source}); costs are shown in {Base}.")]
    DisplayCurrencyHasNoRate,

    [Description("DISPLAY_CURRENCY '{Code}' is not a currency code (e.g. ZAR); ignoring it.")]
    DisplayCurrencyNotACode,

    [Description("{Setting} '{Name}' names no region (use a specific culture such as en-ZA); ignoring it.")]
    CultureHasNoRegion,

    [Description("{Setting} '{Name}' is not a culture .NET knows; ignoring it.")]
    CultureUnknown,

    [Description("Could not write {File}: {Reason}")]
    LedgerWriteFailed,

    [Description("{File} not found at: {Path}")]
    SkillFileNotFound,

    [Description("Loaded {Count} {Kind} skills from: {Path}")]
    SkillsLoaded,

    [Description("Failed to load {File}: {Reason}")]
    SkillFileLoadFailed,

    [Description("Loaded default instructions from: {Path}")]
    DefaultInstructionsLoaded,

    [Description("Could not find default.md in standard locations")]
    DefaultInstructionsNotFound,

    [Description("No personality.md found; running without a personality.")]
    NoPersonality,

    [Description("Failed to connect to Rider at {Endpoint}: {Reason}")]
    McpConnectFailed,

    [Description("Tool discovery error: {Reason}")]
    McpDiscoveryError,

    [Description("Initialize failed: {Status}")]
    McpInitializeFailed,

    [Description("Response: {Body}")]
    McpResponse,

    [Description("Session established: {Session}")]
    McpSessionEstablished,

    [Description("Discovered {Count} tools from Rider")]
    McpToolsDiscovered,

    [Description("Stream discovery failed: {Reason}")]
    McpStreamDiscoveryFailed,

    [Description("SSE response (partial): {Text}")]
    McpSsePartial,

    [Description("SSE discovery failed: {Reason}")]
    McpSseDiscoveryFailed,

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

    [Description("PAYMO_API_KEY not set; Paymo tool disabled.")]
    PaymoKeyMissing,

    [Description("DEVOPS_API_KEY not set; DevOps tool disabled.")]
    DevOpsKeyMissing,

    [Description("DEVOPS_ORGANIZATION and DEVOPS_PROJECT must be set; DevOps tool disabled.")]
    DevOpsSettingsMissing,

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

    [Description("LLM_PROFILE '{Profile}' has no {File}; using the LLM settings from .env and the defaults.")]
    LlmProfileMissing,

    [Description("Display currency: {Display}")]
    DisplayCurrencyChosen,

    [Description("LLM: {Llm}; tools={Tools}; thinking={Thinking}")]
    LlmChosen,

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

    [Description("Prices: {Source} (as of {AsOf}, {Count} models)")]
    PricesLoaded,

    [Description("Prices: {Url} can't be reached ({Reason}); using the price file instead.")]
    PricesUrlUnreachable,

    [Description("No price table at {Path}: usage is recorded without cost. Set PRICES_FILE to price it.")]
    NoPriceTable,

    [Description("Rates: {Source} (as of {AsOf}, {Count} currencies)")]
    RatesLoaded,

    [Description("Rates: {Url} can't be used ({Reason}); using the rates file instead.")]
    RatesUrlUnusable,

    [Description("No exchange rates at {Path}: costs are shown in {Base}.")]
    NoRates,

    [Description("Loaded {Provider}")]
    SkillProviderLoaded,

    [Description("Loaded {Function} skills provider")]
    AgentFunctionLoaded,

    [Description("Loaded {File} via MarkdownSkillProvider")]
    MainSkillsFileLoaded,

    [Description("Skills file '{File}' not found; using default skills only.")]
    MainSkillsFileMissing,

    [Description("Loading ALL skill providers (louis function)")]
    LoadingAllSkills,

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
