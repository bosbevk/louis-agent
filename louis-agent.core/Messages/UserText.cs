namespace LouisAgent.Core.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Text people read: /usage, the usage line, CLI help, comms log headings.</summary>
public enum UserText
{
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

    [Description("\n\nApprove with /approve <Name>, discard with /reject <Name>.")]
    ToolsHint,

    [Description("Usage isn't tracked in this session.")]
    UsageNotTracked,
}
