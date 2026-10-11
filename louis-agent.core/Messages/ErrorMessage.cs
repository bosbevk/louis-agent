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

    [Description("--cache-gaps needs a file: --cache-gaps logs/usage-2026-10.jsonl")]
    CacheGapsNeedsFile,
}
