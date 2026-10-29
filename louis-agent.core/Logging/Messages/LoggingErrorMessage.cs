
using System.ComponentModel;

namespace LouisAgent.Core.Logging.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger).

/// <summary>Exception texts for the logging area: what is wrong and how to fix it.</summary>
public enum LoggingErrorMessage
{
    [Description("Unknown LOG_CONTENT '{Value}'. Use {Supported}.")]
    UnknownLogContent,
}
