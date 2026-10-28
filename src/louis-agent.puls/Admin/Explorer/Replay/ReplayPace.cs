namespace LouisAgent.Puls.Admin.Explorer.Replay;

/// <summary>How long a replay waits before its next event: the events' own rhythm, three times faster.</summary>
public static class ReplayPace
{
    /// <summary>
    /// The wait before an event, from when it came and when the one before it came: a third of the gap, never more
    /// than a quarter second (a tool call's wait), and a short beat when either time is unknown.
    /// </summary>
    public static int WaitMs(DateTimeOffset? at, DateTimeOffset? last) =>
        at is { } now && last is { } before ? (int)Math.Clamp((now - before).TotalMilliseconds / 3, 0, 250) : 15;
}
