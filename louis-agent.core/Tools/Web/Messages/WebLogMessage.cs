namespace LouisAgent.Core.Tools.Web.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Log lines for the web toolset, written to the console and the run file.</summary>
public enum WebLogMessage
{
    [Description("SearXNG searched without these engines: {Engines}")]
    SearxngEnginesSilent,

    [Description("Refused {Request} while rendering {Page}: not a public address")]
    RenderRequestRefused,

    [Description("Couldn't answer {Request} while rendering {Page}: {Reason}")]
    RenderRequestUnhandled,
}
