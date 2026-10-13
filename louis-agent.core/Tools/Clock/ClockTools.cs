namespace LouisAgent.Core.Tools.Clock;

using System.ComponentModel;
using System.Globalization;
using LouisAgent.Core.Messages;

/// <summary>
/// Today's date and the time (F22): a model doesn't know what day it is, so "this week", "recent" or "how long ago" would
/// be guesses. Registered for every session by the engine, like ExecuteSkill, since Puls's own sessions load only the
/// agents toolset. In the user's time zone when they shared it (<see cref="UserPlace"/>), else South African time.
/// </summary>
public sealed class ClockTools(TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    // IANA name first (Linux, and Windows with ICU), then the Windows name; SAST has been UTC+2 without daylight saving since 1944.
    private static readonly TimeZoneInfo SouthAfrica = UserPlace.FindTimeZone("Africa/Johannesburg")
        ?? UserPlace.FindTimeZone("South Africa Standard Time")
        ?? TimeZoneInfo.CreateCustomTimeZone("Africa/Johannesburg", TimeSpan.FromHours(2), "South Africa Standard Time", "SAST");

    [Description("Today's date and the time now where the user is (South Africa unless they shared another time zone), with " +
        "the weekday and the week number. Use it for anything relative to today: 'this week', 'recent', 'how long ago', a deadline.")]
    public string GetToday()
    {
        TimeZoneInfo zone = UserPlace.FindTimeZone(UserPlace.Current?.TimeZone) ?? SouthAfrica;
        DateTimeOffset now = TimeZoneInfo.ConvertTime(_time.GetUtcNow(), zone);
        var invariant = CultureInfo.InvariantCulture;
        return MessageText.Format(ToolMessage.Today, new
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
