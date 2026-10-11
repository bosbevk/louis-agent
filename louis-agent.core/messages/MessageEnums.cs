namespace louis_agent.core.messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by Messages.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception messages.</summary>
public enum ErrorMessage
{
    [Description("{Message} needs a value for '{Hole}'.")]
    MessageNeedsValue,

    [Description("{Message} has no [Description].")]
    MessageHasNoTemplate,

    [Description("Unknown LLM_CACHE '{Value}'. Use on or off.")]
    UnknownCacheSetting,

    [Description("Unknown LLM_CACHE_TTL '{Value}'. Use 5m or 1h.")]
    UnknownCacheTtl,
}

/// <summary>Log lines ([INFO], [WARN]); written to stderr, never stdout.</summary>
public enum LogMessage
{
}

/// <summary>Strings tools return to the model: what went wrong and what to do instead.</summary>
public enum ToolMessage
{
}

/// <summary>Text people read: /usage, the usage line, CLI help, comms log headings.</summary>
public enum UserText
{
}