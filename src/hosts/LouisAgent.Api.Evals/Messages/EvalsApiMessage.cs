using System.ComponentModel;
using LouisAgent.Api.Evals.Messages.Holes;
using LouisAgent.Composition.Messages;

namespace LouisAgent.Api.Evals.Messages;

/// <summary>
/// The eval routes' words: why a start is refused, what a cut-off run says, and the log line of a run that failed
/// (ADR 0009: a text is a member of a message enum, its <c>[Description]</c> a template with named holes).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public enum EvalsApiMessage
{
    [Description("This host has no eval named '{Name}'.")]
    [Holes(typeof(UnknownEvalHoles))]
    UnknownEval,

    [Description("A run starts with its kind, live or replay, and its repeats, 1 or 3.")]
    StartNotRead,

    [Description("A run plays each conversation 1 or 3 times, not {Repeats}.")]
    [Holes(typeof(RepeatsNotOneOrThreeHoles))]
    RepeatsNotOneOrThree,

    [Description("A replay can't be started on this host yet; only a live run can.")]
    ReplayNotYet,

    [Description("This host has nowhere to keep a run: set {Setting} to its data folder.")]
    [Holes(typeof(NowhereToKeepHoles))]
    NowhereToKeep,

    [Description("A run is already going, {Id}. One run goes at a time.")]
    [Holes(typeof(RunGoingHoles))]
    RunGoing,

    [Description("There is no run '{Id}'.")]
    [Holes(typeof(NoSuchRunHoles))]
    NoSuchRun,

    [Description("The run was cut off before it ended, as when the host restarts.")]
    CutOff,

    [Description("The run {Id} failed: {Reason}")]
    [Holes(typeof(RunFailedHoles))]
    RunFailed
}
