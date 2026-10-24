using System.ComponentModel;

namespace LouisAgent.Evals.Messages;

/// <summary>The failures of the conformance check, each a sentence a person reads (ADR 0009: a text is a message enum member).</summary>
public enum ConformanceMessage
{
    [Description("eval name '{Name}' is not safe as an MCP tool name: use letters, digits, '-' and '_', at most 64.")]
    NameNotSafe,

    [Description("eval '{Name}' has version {Version}; versions start at 1.")]
    VersionBelowOne,

    [Description("eval '{Name}' has no required fixed-rule grader.")]
    NoRequiredFixedRule,

    [Description("scenario '{Scenario}' expects '{Key}', which no grader reads.")]
    UnreadKey,

    [Description("scenario '{Scenario}' has no reference transcript and no known-bad one.")]
    NoCase,

    [Description("grader '{Grader}' gave a grade with no reason.")]
    NoReason,

    [Description("grader '{Grader}' scored {Score}, not between 0 and 1.")]
    ScoreOutOfRange,

    [Description("grader '{Grader}' says {Outcome}, which disagrees with its score {Score}.")]
    OutcomeScore,

    [Description("scenario '{Scenario}': the reference transcript fails the required grader '{Grader}': {Reason}")]
    ReferenceFails,

    [Description("scenario '{Scenario}': the known-bad transcript passes every required grader, so the graders can't tell.")]
    BadPasses,

    [Description("grader '{Grader}' threw: {Reason}.")]
    GraderThrew,
}
