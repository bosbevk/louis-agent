using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>
/// The texts of the drafts a person makes in the admin portal: what a draft can't be made without (ADR 0009).
/// </summary>
public enum DraftMessage
{
    [Description(
        "A known-bad transcript needs a reason, in kebab-case: its file is named {Scenario}.bad.<reason>.txt, so the name says what went wrong.")]
    KnownBadNeedsAReason,
}
