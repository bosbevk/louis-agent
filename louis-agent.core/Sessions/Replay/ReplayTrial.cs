using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Messages;

namespace LouisAgent.Core.Sessions.Replay;

/// <summary>The trial this flow records into or replays from (F18 step 4b), or null when the session's web is off or unset.</summary>
public sealed record ReplayTrial(WebReplayMode Mode, string Of)
{
    public static ReplayTrial? Current => SessionReplay.Current switch
    {
        null or { Web: WebReplayMode.Off } => null,
        { Of: { } of } replay => new ReplayTrial(replay.Web, of),
        { } replay => throw new InvalidOperationException(MessageText.Format(SessionErrorMessage.NeedsTrial, new { Mode = replay.Web })),
    };

    public bool Replaying => Mode == WebReplayMode.Replay;
}
