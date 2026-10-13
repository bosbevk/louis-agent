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

    [Description("'{Name}' isn't a personality name: use lowercase letters, digits and dashes.")]
    PersonalityNameInvalid,

    [Description("Unknown personality '{Name}'. Available: {Available}.")]
    UnknownPersonality,

    [Description("Toolset '{Name}' needs {Settings} to be set on this instance.")]
    ToolsetSettingsMissing,


    [Description("depth must be between 0 and {Max}: sub-sessions are one level deep.")]
    SessionDepthInvalid,

    [Description("A sub-session can't load the '{Toolset}' toolset: sub-sessions are one level deep.")]
    SubSessionCannotDelegate,
}
