namespace LouisAgent.Core.Logging.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the logging itself, written to the console and the run file.</summary>
public enum LoggingLogMessage
{
    [Description("{Tool} returned {Chars} characters, {Outcome} to {ReturnedChars}; arguments {Arguments}; request: {UserRequest}")]
    OversizedToolResult,

    [Description("{Tool} was cut off by the output limit ({MaxOutputTokens} tokens); arguments {Arguments}")]
    TruncatedToolCall,

    [Description("Logging to {Path}")]
    LoggingTo,

    [Description("File logging disabled; could not use '{Directory}': {Reason}")]
    FileLoggingDisabled,
}
