namespace LouisAgent.Core.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

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

    [Description("turn: {Usage}")]
    UsageTurn,

    [Description("{Line} · session {Cost}")]
    UsageWithSession,

    [Description("No model requests in this session yet.")]
    UsageNoneYet,

    [Description("Session usage: {Usage}")]
    UsageSessionHeading,

    [Description("- {Purpose}: {Usage}")]
    UsagePurpose,

    [Description("- tokens: {Input} input · {CacheWrite} cache write · {CacheRead} cache read · {Output} output")]
    UsageTokensByKind,

    [Description("- rate: {Rate} on {AsOf} · {Charged} charged")]
    UsageRate,

    [Description("- price unknown for {Requests}: the model isn't in the price table")]
    UsageUnpriced,

    [Description("{Requests} · {Tokens} tokens{Cached} · {Cost}")]
    UsageTotal,

    [Description(" ({Tokens} cached)")]
    UsageCached,

    [Description("price unknown")]
    UsagePriceUnknown,

    [Description("{Amount} + {Unpriced} unpriced")]
    UsagePartlyPriced,

    [Description("1 request")]
    UsageOneRequest,

    [Description("{Count} requests")]
    UsageRequests,

    [Description(" and removed it from this session")]
    ToolRejectedFromSession,

    [Description("Tool building is not available.")]
    ToolBuildingNotAvailable,

    [Description("Usage: /approve <ToolName>")]
    ApproveUsage,

    [Description("Usage: /reject <ToolName>")]
    RejectUsage,

    [Description("No pending tool named '{Name}'. Use /tools to see pending tools.")]
    NoPendingToolSeeTools,

    [Description("Cannot approve '{Name}': the definition is invalid ({Errors}).")]
    CannotApprove,

    [Description("An approved tool '{Name}' already exists; remove {Path} first.")]
    ApprovedToolAlreadyThere,

    [Description("Approved '{Name}'. Saved to {Path}; it will load in every future session.")]
    ToolApproved,

    [Description("Rejected '{Name}': deleted the pending definition{Removed}.")]
    ToolRejected,

    [Description("No pending tool named '{Name}'.")]
    NoPendingTool,
}
