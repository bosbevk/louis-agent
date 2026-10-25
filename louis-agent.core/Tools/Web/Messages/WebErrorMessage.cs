namespace LouisAgent.Core.Tools.Web.Messages;

using System.ComponentModel;

// The message enums (docs/CLAUDE.md, *Messages and logging*). Each member is named for what happened, and its
// [Description] is the text, with named holes filled by MessageText.Format (or by the structured logger). Members are added
// as code is written or moved; the refactor after F4 moves the existing messages here.

/// <summary>
/// The exception texts of recorded and replayed web input (F18 step 4a; ADR 0009), for the developer: a model never reads them (what it
/// reads when a replay misses is <see cref="WebToolMessage.ReplayMiss"/>). Step 4b adds its web-replay texts here.
/// </summary>
public enum WebErrorMessage
{
    [Description("Web replay mode '{Mode}' needs the recorded trial to use (SessionReplay.Of); the API resolves it when a session opens.")]
    NeedsTrial,

    [Description("'{Name}' would sit outside the replay folder: trial files are named only by TrialFileName.")]
    OutsideFolder,
}
