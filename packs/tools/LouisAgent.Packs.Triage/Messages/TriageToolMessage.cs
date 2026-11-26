using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Triage.Messages.Holes.Cases;
using LouisAgent.Packs.Triage.Messages.Holes.Runs;

namespace LouisAgent.Packs.Triage.Messages;

/// <summary>
/// The triage pack's texts the model reads (ADR 0009), pinned by <c>TriageToolMessageSnapshot.json</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum TriageToolMessage
{
    [Description("'{RunId}' is not a run id: letters, digits, dashes and underscores only, up to 64 of them.")]
    [Holes(typeof(BadRunIdHoles))]
    BadRunId,

    [Description("There is no run '{RunId}'. Use the run id you were given.")]
    [Holes(typeof(NoSuchRunHoles))]
    NoSuchRun,

    [Description("'{Value}' is not a {What}: one of {Options}.")]
    [Holes(typeof(NotAChoiceHoles))]
    NotAChoice,

    [Description("A {What} needs a reason, in a sentence.")]
    [Holes(typeof(NoReasonHoles))]
    NoReason,

    [Description("Recorded step {Number} of run {RunId}: {Summary}")]
    [Holes(typeof(RecordedHoles))]
    Recorded,

    [Description("Raised run {RunId} to a person: the case is written at {Path}, and step {Number} says so.")]
    [Holes(typeof(RaisedHoles))]
    Raised,

    [Description("Run {RunId}: {Count} steps so far{Ended}.\n{Steps}")]
    [Holes(typeof(RunReadHoles))]
    RunRead,

    [Description(", ended {Outcome}")]
    [Holes(typeof(RunEndedHoles))]
    RunEnded,

    [Description("{Number}. {Agent} ({Kind}, {At}): {Summary} {Why}")]
    [Holes(typeof(StepLineHoles))]
    StepLine,

    [Description(
        "'{Name}' in {File} is not a department name, or repeats one: letters, digits, dashes and underscores, " +
        "each once.")]
    BadDepartment,
}
