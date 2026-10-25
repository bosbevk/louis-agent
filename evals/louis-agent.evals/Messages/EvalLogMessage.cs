using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The eval's log lines (ADR 0009: a text is a message enum member with its template).</summary>
public enum EvalLogMessage
{
    [Description("Eval '{Eval}' runs {Modes} mode(s), {Repeats} repeat(s), as run {Run}.")]
    RunStarted,

    [Description("Scenario '{Scenario}' has no messages, so it is skipped.")]
    NoMessages,

    [Description("Could not close session '{Session}', so the run goes on: {Reason}.")]
    CloseFailed,

    [Description("Grader '{Grader}' of eval '{Eval}' threw on scenario '{Scenario}', so its grade is Unknown and the run goes on: {Reason}.")]
    GraderThrew,

    [Description("Could not open the session for '{Scenario}' ({Failure}), so every turn of it is recorded as failed: {Reason}.")]
    OpenFailed,

    [Description("Turn {Turn} of '{Scenario}' failed ({Failure}), recorded, and the run goes on: {Reason}.")]
    TurnFailed,
}
