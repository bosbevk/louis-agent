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
}
