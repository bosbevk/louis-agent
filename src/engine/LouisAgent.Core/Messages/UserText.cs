using LouisAgent.Composition.Messages;
using LouisAgent.Core.Messages.Holes.Approvals;
using LouisAgent.Core.Messages.Holes.Drafts;
using LouisAgent.Core.Messages.Holes.SkillFiles;
using LouisAgent.Core.Messages.Holes.UsageFigures;
using LouisAgent.Core.Messages.Holes.UsageLines;
using System.ComponentModel;

namespace LouisAgent.Core.Messages;

/// <summary>
/// Text people read: /usage, the usage line, the replies to /approve and /reject, CLI help, comms log headings. Each
/// member is named for what happened, and its [Description] is the text, with named holes filled by
/// MessageText.Format from the record its [Holes] names (src/docs/CLAUDE.md, *Messages and logging*).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0119-declare-name-then-pass.md">ADR 0119: named types, declared, then passed</seealso>
public enum UserText
{
    [Description("turn: {Usage}")]
    [Holes(typeof(UsageTurnHoles))]
    UsageTurn,

    [Description("{Line} · session {Cost}")]
    [Holes(typeof(UsageWithSessionHoles))]
    UsageWithSession,

    [Description("No model requests in this session yet.")]
    UsageNoneYet,

    [Description("Session usage: {Usage}")]
    [Holes(typeof(UsageSessionHeadingHoles))]
    UsageSessionHeading,

    [Description("- {Purpose}: {Usage}")]
    [Holes(typeof(UsagePurposeHoles))]
    UsagePurpose,

    [Description("- tokens: {Input} input · {CacheWrite} cache write · {CacheRead} cache read · {Output} output")]
    [Holes(typeof(UsageTokensByKindHoles))]
    UsageTokensByKind,

    [Description("- rate: {Rate} on {AsOf} · {Charged} charged")]
    [Holes(typeof(UsageRateHoles))]
    UsageRate,

    [Description("- price unknown for {Requests}: the model isn't in the price table")]
    [Holes(typeof(UsageUnpricedHoles))]
    UsageUnpriced,

    [Description("{Requests} · {Tokens} tokens{Cached} · {Cost}")]
    [Holes(typeof(UsageTotalHoles))]
    UsageTotal,

    [Description(" ({Tokens} cached)")]
    [Holes(typeof(UsageCachedHoles))]
    UsageCached,

    [Description("price unknown")] UsagePriceUnknown,

    [Description("{Amount} + {Unpriced} unpriced")]
    [Holes(typeof(UsagePartlyPricedHoles))]
    UsagePartlyPriced,

    [Description("1 request")] UsageOneRequest,

    [Description("{Count} requests")]
    [Holes(typeof(UsageRequestsHoles))]
    UsageRequests,

    [Description(" and stopped it: it no longer runs, in an open chat or the next")]
    ToolRejectedFromSession,

    [Description("Tool building is not available.")]
    ToolBuildingNotAvailable,

    [Description("Usage: /approve <Name>")]
    ApproveUsage,

    [Description("Usage: /reject <Name>")] RejectUsage,

    [Description("No pending tool or skill file named '{Name}'. Use /tools to see what is pending.")]
    [Holes(typeof(NoPendingToolSeeToolsHoles))]
    NoPendingToolSeeTools,

    [Description("Cannot approve '{Name}': the definition is invalid ({Errors}).")]
    [Holes(typeof(CannotApproveHoles))]
    CannotApprove,

    [Description("An approved tool '{Name}' already exists; remove {Path} first.")]
    [Holes(typeof(ApprovedToolAlreadyThereHoles))]
    ApprovedToolAlreadyThere,

    [Description("Approved '{Name}'. Saved to {Path}; it loads in every future session, from your next chat on.")]
    [Holes(typeof(ToolApprovedHoles))]
    ToolApproved,

    [Description("Rejected '{Name}': deleted the pending definition{Removed}.")]
    [Holes(typeof(ToolRejectedHoles))]
    ToolRejected,

    [Description("No pending tool or skill file named '{Name}'.")]
    [Holes(typeof(NoPendingToolHoles))]
    NoPendingTool,

    [Description("\n\nApprove with /approve <Name>, discard with /reject <Name>.")]
    ToolsHint,

    [Description(
        "Approved skill file '{Name}'. Moved to {Path}; every later session with the skills toolset loads it, from " +
        "your next chat on.")]
    [Holes(typeof(SkillFileApprovedHoles))]
    SkillFileApproved,

    [Description("Rejected skill file '{Name}': deleted {Path}{Removed}.")]
    [Holes(typeof(SkillFileRejectedHoles))]
    SkillFileRejected,

    [Description(" and unloaded it from this session")]
    SkillFileRejectedFromSession,

    [Description("- {Name} [skill file pending, active here]")]
    [Holes(typeof(PendingSkillFileActiveHoles))]
    PendingSkillFileActive,

    [Description("- {Name} [skill file pending from an earlier session, not loaded]")]
    [Holes(typeof(PendingSkillFileNotLoadedHoles))]
    PendingSkillFileNotLoaded,

    [Description(
        "'{Name}' is both a pending tool ({Tool}) and a pending skill file ({File}); nothing was done. Reject one of " +
        "them first, then approve the other.")]
    [Holes(typeof(PendingToolAndSkillFileHoles))]
    PendingToolAndSkillFile,

    [Description("Usage isn't tracked in this session.")]
    UsageNotTracked,

    [Description(
        "Approved {Name}: this chat can use it now through UseSkill, and every chat from the next lists it.")]
    [Holes(typeof(DraftApprovedHoles))]
    DraftApproved,

    [Description("Rejected {Name}: its draft is deleted, and no chat can use it.")]
    [Holes(typeof(DraftRejectedHoles))]
    DraftRejected,

    [Description("Drafts waiting for /approve or /reject:")]
    DraftsHeading,

    [Description("- {Name}: {Description} Proved with: {Example}")]
    [Holes(typeof(DraftListedHoles))]
    DraftListed,

    [Description("No drafts are waiting.")]
    NoDraftsWaiting,

    [Description("Approved skills: {Names}.")]
    [Holes(typeof(ApprovedSkillsListedHoles))]
    ApprovedSkillsListed,
}
