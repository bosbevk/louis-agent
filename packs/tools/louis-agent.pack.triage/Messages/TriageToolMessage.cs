using System.ComponentModel;

namespace LouisAgent.Packs.Triage.Messages;

/// <summary>The triage pack's texts the model reads (ADR 0009), pinned by <c>TriageToolMessageSnapshot.json</c>.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum TriageToolMessage
{
    [Description("'{RunId}' is not a run id: letters, digits, dashes and underscores only, up to 64 of them.")]
    BadRunId,

    [Description("There is no run '{RunId}'. Use the run id you were given.")]
    NoSuchRun,

    [Description("'{Value}' is not a {What}: one of {Options}.")]
    NotAChoice,

    [Description("A {What} needs a reason, in a sentence.")]
    NoReason,

    [Description("Recorded step {Number} of run {RunId}: {Summary}")]
    Recorded,
}
