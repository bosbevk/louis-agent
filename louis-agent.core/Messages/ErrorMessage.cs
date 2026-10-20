namespace LouisAgent.Core.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception messages.</summary>
public enum ErrorMessage
{
    [Description("{Message} needs a value for '{Hole}'.")]
    MessageNeedsValue,

    [Description("{Message} has no [Description].")]
    MessageHasNoTemplate,

    [Description("Two tools are named '{Name}'; tool names must be unique across all toolsets.")]
    DuplicateToolName,

    [Description("Unknown LLM_CACHE '{Value}'. Use on or off.")]
    UnknownCacheSetting,

    [Description("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h.")]
    UnknownCacheTtl,

    [Description("Unknown LLM_THINKING '{Value}'. Use off, low, medium or high.")]
    UnknownThinking,

    [Description("Unknown AGENT_AUTH '{Value}'. Use {Supported}.")]
    UnknownAuthMode,

    [Description("{Setting} is required: set it in config/.env.secrets, or set AGENT_AUTH=off to serve without a key on purpose.")]
    ApiKeyRequired,

    [Description("'{Key}' is not a setting name.")]
    NotASettingName,

    [Description("A setting's value is one line.")]
    SettingValueNotOneLine,

    [Description("Exchange rates {Source} are not in rates.json format: {Reason}")]
    RatesNotRatesJson,

    [Description("Exchange rates {Source} are empty.")]
    RatesEmpty,

    [Description("Exchange rates {Source}: base '{Base}' is not a currency code.")]
    RatesBaseNotACurrency,

    [Description("Exchange rates {Source}: '{Code}' is not a currency code.")]
    RatesCodeNotACurrency,

    [Description("Exchange rates {Source}: the {Code} rate must be positive.")]
    RateNotPositive,

    [Description("Frankfurter rates {Source}: amount must be positive.")]
    FrankfurterAmountNotPositive,

    [Description("Frankfurter rates {Source} are not in Frankfurter's format: {Reason}")]
    FrankfurterNotItsFormat,

    [Description("Price table {Source} is not valid: {Reason}")]
    PriceTableNotValid,

    [Description("Price table {Source} is empty.")]
    PriceTableEmpty,

    [Description("Price table {Source}: '{Model}' has a negative {Price} price.")]
    PriceTableNegativePrice,

    [Description("Price table {Source}: '{Model}' long_prompt.above_tokens must be positive.")]
    PriceTableLongPromptNotPositive,

    [Description("{Path} line {Number} is not a usage record: {Reason}")]
    NotAUsageRecord,

    [Description("LLM_MODEL is required.")]
    LlmModelRequired,

    [Description("Unknown LLM_PROVIDER '{Provider}'. Supported: {Supported}.")]
    UnknownProvider,

    [Description("ANTHROPIC_API_KEY (or LLM_API_KEY) is required for provider 'anthropic'.")]
    AnthropicKeyRequired,

    [Description("LLM_ENDPOINT is required for provider 'openai-compatible'.")]
    OpenAiEndpointRequired,

    [Description("Workspace root cannot be empty.")]
    WorkspaceRootEmpty,

    [Description("This host only exposes tools; it has no language model.")]
    ToolHostHasNoModel,

    [Description("PRICES_URL must be an http or https URL, got '{Url}'.")]
    PricesUrlInvalid,

    [Description("RATES_URL must be an http or https URL (or off), got '{Url}'.")]
    RatesUrlInvalid,

    [Description("Exchange rates {Source} are from {Base}, but the price table is in {Currency}: set RATES_URL to rates from {Currency}.")]
    RatesWrongBase,

    [Description("RATES_FORMAT must be '{Frankfurter}' or '{Json}', got '{Format}'.")]
    RatesFormatUnknown,

    [Description("Unknown toolset(s): {Names}. Valid names: {Valid}.")]
    UnknownToolsets,

    [Description("'{Name}' isn't a role name: use lowercase letters, digits and dashes, and one / between a product and its mode.")]
    RoleNameInvalid,

    [Description("Unknown role '{Name}'. Available: {Available}.")]
    UnknownRole,

    [Description("Toolset '{Name}' needs {Settings} to be set on this instance.")]
    ToolsetSettingsMissing,


    [Description("depth must be between 0 and {Max}: a specialist is one level down, a minion two.")]
    SessionDepthInvalid,

    [Description("A sub-session can't load the '{Toolset}' toolset: only the agent the user talks to delegates to specialists.")]
    SubSessionCannotDelegate,

    [Description("A minion opens with a role (depth 2 names one). Available: {Available}.")]
    MinionNeedsRole,

    [Description("No '{Tool}' tool in the toolsets {Toolsets}.")]
    ToolNotInToolsets,

    [Description("A minion (depth {Depth}) names the one tool it gets: 'tool' is required.")]
    MinionNeedsTool,

    [Description("'tool' is for a minion (depth {MinionDepth}): a session at depth {Depth} loads toolsets.")]
    ToolOnlyForMinions,

    [Description("A minion needs its parent: 'parent' names the specialist's session and turn ({Example}).")]
    MinionNeedsParent,

    [Description("Unknown parent session '{Session}': a minion runs on its specialist's own instance.")]
    MinionParentUnknown,

    [Description("'{Tool}' is never handed to a minion.")]
    ToolNeverHandedDown,

    [Description("The parent session has no '{Tool}' tool: a minion gets one of its specialist's own tools.")]
    ParentLacksTool,

    [Description("MINION_LLMS names '{Name}' for '{Role}', which is neither a profile in LLM_SESSION_PROFILES ({Available}) nor 'instance'.")]
    MinionLlmUnknown,

    [Description("The 'minions' toolset is a specialist's (depth 1): the agent the user talks to delegates to specialists, and a minion delegates to no one.")]
    MinionsOnlyForSpecialists,

    [Description("No toolset builds a '{Tool}' tool.")]
    NoToolsetForTool,


    [Description("Unknown LLM profile '{Name}'. Available: {Available}.")]
    UnknownSessionLlm,

    [Description("'{Name}' is registered twice as a {Kind}: a pack, toolset, role or prompt path has one home.")]
    CatalogNameTwice,

    [Description("'{Owner}' names the pack '{Name}', which isn't registered. Registered: {Registered}.")]
    CatalogPackUnknown,

    [Description("Packs require each other in a cycle: {Cycle}.")]
    CatalogRequiresCycle,

    [Description("Tool '{Tool}' is in two toolsets, '{First}' and '{Second}'; tool names are unique across all toolsets.")]
    CatalogToolNameTwice,

    [Description("Tool '{Tool}' in toolset '{Toolset}' takes the engine's own tool name.")]
    CatalogToolNameIsTheEngines,

    [Description("Prompts were added after the first one was read: register every identity and pack before the host builds.")]
    PromptsAddedAfterRead,

    [Description("ROLES_ENABLED names roles this instance doesn't have: {Names}. Available: {Available}.")]
    UnknownRolesEnabled,

    [Description("The default mode '{Mode}' has no file ({File}): a session that names no role would open with no mode.")]
    DefaultModeMissing,

    [Description("HOST_TIME_ZONE '{Value}' isn't a time zone this machine knows: use an IANA name such as Europe/London, or leave it unset for the machine's own.")]
    UnknownHostTimeZone,

    [Description("LLM_SESSION_PROFILES names '{Name}', but {File} isn't there.")]
    SessionLlmFileMissing,

    [Description("'{Name}' isn't an LLM profile name: use lowercase letters, digits and dashes.")]
    SessionLlmNameInvalid,
}
