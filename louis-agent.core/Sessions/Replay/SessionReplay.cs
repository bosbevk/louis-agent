using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions.Messages;

namespace LouisAgent.Core.Sessions.Replay;

/// <summary>
/// What an eval session does with the web (F18 step 4a). <paramref name="Of"/> is the recorded trial this session writes
/// (<see cref="WebReplayMode.Record"/>) or reads (<see cref="WebReplayMode.Replay"/>), resolved by the API when the session
/// opens: a recording uses <c>{run tag}/{task}</c>, a replay the <c>of</c> it was sent, and a sub-session is sent its parent's, so
/// one scenario's pages never mix with another's. <paramref name="Model"/> asks for the model's answers to replay too (step 4e's
/// cache; the API refuses it until then). Held in an <see cref="AsyncLocal{T}"/> as <see cref="UserPlace"/> is: none by default,
/// and a host sets it only for a session that carries a run tag.
/// </summary>
public sealed record SessionReplay(WebReplayMode Web, string? Of, bool Model)
{
    private static readonly AsyncLocal<SessionReplay?> CurrentReplay = new();

    /// <summary>The replay settings of this flow, if a host set any.</summary>
    public static SessionReplay? Current => CurrentReplay.Value;

    /// <summary>Makes <paramref name="replay"/> this flow's until the result is disposed.</summary>
    public static IDisposable Use(SessionReplay? replay)
    {
        var previous = CurrentReplay.Value;
        CurrentReplay.Value = replay;
        return new Restore(previous);
    }

    /// <summary>
    /// The <c>replay</c> a session sends when it opens a sub-session (F18 step 4a): this flow's settings as the API reads them
    /// (<c>web</c> in lower case, <c>of</c>, <c>model</c>), so the child records into, or replays from, the parent's own trial and
    /// never works out a key of its own; null when this flow has none.
    /// </summary>
    public static object? ForSubSession() =>
        Current is { } replay ? new { web = replay.Web.ToString().ToLowerInvariant(), of = replay.Of, model = replay.Model } : null;

    /// <summary>WEB_REPLAY, parsed once where it enters: unset is <see cref="WebReplayMode.Off"/>; anything unknown throws.</summary>
    public static WebReplayMode DefaultMode(string? setting)
    {
        if (!SettingChoice.TryParse(setting, out WebReplayMode? mode))
            throw new ArgumentException(MessageText.Format(SessionErrorMessage.UnknownWebReplay,
                new { Value = setting?.Trim(), Supported = string.Join(" or ", SettingChoice.Names<WebReplayMode>()) }));
        return mode ?? WebReplayMode.Off;
    }

    private sealed class Restore(SessionReplay? previous) : IDisposable
    {
        public void Dispose() => CurrentReplay.Value = previous;
    }
}
