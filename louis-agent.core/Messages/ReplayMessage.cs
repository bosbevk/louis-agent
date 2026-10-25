using System.ComponentModel;

namespace LouisAgent.Core.Messages;

/// <summary>
/// The exception texts of recorded and replayed web input (F18 step 4a; ADR 0009), for the developer: a model never reads them (what it
/// reads when a replay misses is <see cref="ToolMessage.ReplayMiss"/>). Their own enum, because <see cref="ErrorMessage"/> is at its
/// 200 lines; 4b and 4e add theirs here.
/// </summary>
public enum ReplayMessage
{
    [Description("Web replay mode '{Mode}' needs the recorded trial to use (SessionReplay.Of); the API resolves it when a session opens.")]
    NeedsTrial,

    [Description("'{Name}' would sit outside the replay folder: trial files are named only by TrialFileName.")]
    OutsideFolder,
}
