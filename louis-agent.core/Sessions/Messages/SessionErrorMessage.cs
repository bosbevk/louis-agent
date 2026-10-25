namespace LouisAgent.Core.Sessions.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the sessions area: what is wrong and how to fix it.</summary>
public enum SessionErrorMessage
{
    [Description("Unknown WEB_REPLAY '{Value}'. Use {Supported}.")]
    UnknownWebReplay,
}
