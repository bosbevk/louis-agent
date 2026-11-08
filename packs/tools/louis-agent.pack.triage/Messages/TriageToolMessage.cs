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

    [Description("Raised run {RunId} to a person: the case is written at {Path}, and step {Number} says so.")]
    Raised,

    [Description("Run {RunId}: {Count} steps so far{Ended}.\n{Steps}")]
    RunRead,

    [Description(", ended {Outcome}")] RunEnded,

    [Description("{Number}. {Agent} ({Kind}, {At}): {Summary} {Why}")]
    StepLine,

    [Description(
        "'{Name}' in {File} is not a department name, or repeats one: letters, digits, dashes and underscores, each once.")]
    BadDepartment,
}
