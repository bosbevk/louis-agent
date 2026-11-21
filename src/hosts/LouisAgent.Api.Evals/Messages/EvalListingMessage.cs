using System.ComponentModel;

namespace LouisAgent.Api.Evals.Messages;

/// <summary>
/// Why one of the runner's listings (<c>--list</c>, <c>--checks</c>) couldn't be read (ADR 0009: a text is a message
/// enum member).
/// </summary>
public enum EvalListingMessage
{
    [Description("the eval runner isn't at {Path}")]
    Missing,

    [Description("the eval runner's {Command} didn't finish within {Seconds} s")]
    TimedOut,

    [Description("the eval runner's {Command} exited with {Code}")]
    ExitCode,

    [Description("the eval runner's {Command} exited with {Code}: {Reason}")]
    ExitCodeWithReason,

    [Description("a line of the eval runner's --list isn't a JSON eval: {Line}")]
    NotAnEval,

    [Description("a line of the eval runner's --checks isn't a check or a reader: {Line}")]
    NotACheck,
}
