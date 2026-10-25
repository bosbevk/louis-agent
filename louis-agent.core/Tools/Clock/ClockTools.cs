namespace LouisAgent.Core.Tools.Clock;

using System.ComponentModel;
using LouisAgent.Core.Sessions;
using System.Globalization;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Clock.Messages;

/// <summary>
/// Today's date and the time (F22): a model doesn't know what day it is, so "this week", "recent" or "how long ago" would
/// be guesses. Registered for every session by the engine, like ExecuteSkill, since a chat's sessions may load no
/// toolset that has it. In the user's time zone when they shared it (<see cref="UserPlace"/>), else the host's
/// (<c>HOST_TIME_ZONE</c>, CL15; core names no country), else the machine's own.
/// </summary>
public sealed class ClockTools(TimeProvider? time = null, string? hostTimeZone = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    // A setting that names no zone this machine knows falls back to the machine's own, not to a crash at start-up.
    private readonly TimeZoneInfo _host = UserPlace.FindTimeZone(hostTimeZone) ?? TimeZoneInfo.Local;

    [Description("Today's date and the time now where the user is (the host's time zone unless they shared another), with " +
        "the weekday and the week number. Use it for anything relative to today: 'this week', 'recent', 'how long ago', a deadline.")]
    public string GetToday()
    {
        TimeZoneInfo zone = UserPlace.FindTimeZone(UserPlace.Current?.TimeZone) ?? _host;
        DateTimeOffset now = TimeZoneInfo.ConvertTime(_time.GetUtcNow(), zone);
        var invariant = CultureInfo.InvariantCulture;
        return MessageText.Format(ClockToolMessage.Today, new
        {
            Weekday = now.ToString("dddd", invariant),
            Date = now.ToString("d MMMM yyyy", invariant),
            Time = now.ToString("HH:mm", invariant),
            Zone = zone.Id,
            Offset = now.ToString("zzz", invariant),
            Iso = now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", invariant),
            Week = ISOWeek.GetWeekOfYear(now.DateTime),
        });
    }
}
