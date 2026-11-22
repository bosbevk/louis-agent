using LouisAgent.Composition.Messages;
using System.ComponentModel;
using LouisAgent.Composition.Messages.Holes.Errors;
using LouisAgent.Composition.Messages.Holes.Files;
using LouisAgent.Composition.Messages.Holes.Processes;

namespace LouisAgent.Composition.Messages;
// The message enums (src/docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are
// added as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings tools return to the model: what went wrong and what to do instead.</summary>
public enum ToolMessage
{
    [Description("Couldn't read {Path} as a document: {Reason}")]
    [Holes(typeof(DocumentUnreadableHoles))]
    DocumentUnreadable,

    [Description("Error: '{Path}' is outside the workspace.")]
    [Holes(typeof(PathOutsideWorkspaceNamedHoles))]
    PathOutsideWorkspaceNamed,

    [Description("[Exit Code {Code}] {Output}")]
    [Holes(typeof(ExitCodeHoles))]
    ExitCode,

    [Description("Error: {Reason}")]
    [Holes(typeof(ErrorWithReasonHoles))]
    ErrorWithReason,

    [Description("Error running {Program}: {Reason}")]
    [Holes(typeof(ProcessStartFailedHoles))]
    ProcessStartFailed,

    [Description("Reading this sensitive file is not allowed.")]
    SensitiveRead,

    [Description("File not found: {Path}")]
    [Holes(typeof(FileNotFoundHoles))]
    FileNotFound,

    [Description("Success (no output).")] SucceededNoOutput,

    [Description("Program was still running after {Seconds}s and was killed.")]
    [Holes(typeof(ProgramKilledHoles))]
    ProgramKilled,

    [Description("Exit code: {ExitCode}")]
    [Holes(typeof(ProcessExitCodeHoles))]
    ProcessExitCode,
}
