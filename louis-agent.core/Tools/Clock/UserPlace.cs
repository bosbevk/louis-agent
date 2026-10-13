namespace LouisAgent.Core.Tools.Clock;

/// <summary>
/// Where the user is (F22): the place they set (a town, as they wrote it or as the browser found it) and their time
/// zone. The host sets it for a turn; <see cref="ClockTools.GetToday"/> tells the time in that zone, and
/// <see cref="Agents.AgentPeerTools"/> passes it on to a specialist. Held in an <see cref="AsyncLocal{T}"/>; none by
/// default, and a host only sets it when the user chose to share it.
/// </summary>
/// <param name="Location">The user's place, e.g. "Pretoria, Gauteng, Suid-Afrika"; null when not shared.</param>
/// <param name="TimeZone">An IANA time zone (Africa/Johannesburg); null for South African time.</param>
public sealed record UserPlace(string? Location, string? TimeZone)
{
    private static readonly AsyncLocal<UserPlace?> CurrentPlace = new();

    /// <summary>The user's place in this flow, if a host set one.</summary>
    public static UserPlace? Current => CurrentPlace.Value;

    /// <summary>Makes <paramref name="place"/> this flow's place until the result is disposed.</summary>
    public static IDisposable Use(UserPlace? place)
    {
        var previous = CurrentPlace.Value;
        CurrentPlace.Value = place;
        return new Restore(previous);
    }

    /// <summary>The time zone named by <paramref name="id"/>, or null when this system doesn't know it.</summary>
    public static TimeZoneInfo? FindTimeZone(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { return null; }
    }

    private sealed class Restore(UserPlace? previous) : IDisposable
    {
        public void Dispose() => CurrentPlace.Value = previous;
    }
}
