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

    [Description("--cache-gaps needs a file: --cache-gaps logs/usage-2026-10.jsonl")]
    CacheGapsNeedsFile,
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
    [Description("{Path}: the prefix's cache lifetime (F4), per host and model; prices as of {AsOf}")]
    CacheGapsHeader,

    [Description("No requests that carry the prefix in {Path}.")]
    CacheGapsNone,

    [Description("{Host} · {Model} · {Requests} requests · prefix ≈ {Prefix} tokens (its smallest prompt)")]
    CacheGapsGroup,

    [Description("  gaps: {Under5m} under 5 min · {From5To60m} from 5 to 60 min · {Over60m} over an hour")]
    CacheGapsGaps,

    [Description("  {Lifetime}: writes {Writes}, reads {Reads} → {Cost}")]
    CacheGapsLifetime,

    [Description("  → LLM_CACHE_TTL=1h: saves {Saving} ({Percent}) on the prefix")]
    CacheGapsRecommendOneHour,

    [Description("  → LLM_CACHE_TTL=5m (the default): 1 hour would cost {Extra} more")]
    CacheGapsRecommendFiveMinutes,

    [Description("  no price for {Model}: no recommendation")]
    CacheGapsNoPrice,
}