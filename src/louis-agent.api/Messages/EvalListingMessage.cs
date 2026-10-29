using System.ComponentModel;

namespace LouisAgent.Api.Messages;

/// <summary>Why the runner's listing couldn't be read (ADR 0009: a text is a message enum member).</summary>
public enum EvalListingMessage
{
    [Description("the eval runner isn't at {Path}")]
    Missing,

    [Description("the eval runner's --list didn't finish within {Seconds} s")]
    TimedOut,

    [Description("the eval runner's --list exited with {Code}")]
    ExitCode,

    [Description("the eval runner's --list exited with {Code}: {Reason}")]
    ExitCodeWithReason,

    [Description("a line of the eval runner's --list isn't a JSON eval: {Line}")]
    NotAnEval,
}
