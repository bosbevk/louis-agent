
using System.ComponentModel;

namespace LouisAgent.Core.Sessions.Messages;
// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>Exception texts for the sessions area: what is wrong and how to fix it.</summary>
public enum SessionErrorMessage
{
    [Description("Unknown WEB_REPLAY '{Value}'. Use {Supported}.")]
    UnknownWebReplay,

    [Description(
        "Web replay mode '{Mode}' needs the recorded trial to use (SessionReplay.Of); the API resolves it when a session opens.")]
    NeedsTrial,

    [Description("'{Name}' would sit outside the replay folder: trial files are named only by TrialFileName.")]
    OutsideFolder,
}
