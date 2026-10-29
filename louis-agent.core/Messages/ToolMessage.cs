
using System.ComponentModel;

namespace LouisAgent.Core.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Strings tools return to the model: what went wrong and what to do instead.</summary>
public enum ToolMessage
{
    [Description("Couldn't read {Path} as a document: {Reason}")]
    DocumentUnreadable,

    [Description("Error: '{Path}' is outside the workspace.")]
    PathOutsideWorkspaceNamed,

    [Description("[Exit Code {Code}] {Output}")]
    ExitCode,

    [Description("Error: {Reason}")] ErrorWithReason,

    [Description("Error running {Program}: {Reason}")]
    ProcessStartFailed,

    [Description("Reading this sensitive file is not allowed.")]
    SensitiveRead,

    [Description("File not found: {Path}")]
    FileNotFound,

    [Description("Success (no output).")] SucceededNoOutput,

    [Description("Program was still running after {Seconds}s and was killed.")]
    ProgramKilled,

    [Description("Exit code: {ExitCode}")] ProcessExitCode,
}
