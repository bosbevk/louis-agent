namespace LouisAgent.Core.Tools.Clock;

using System.ComponentModel;
using System.Globalization;
using LouisAgent.Core.Messages;

/// <summary>
/// Today's date and the time (F22): a model doesn't know what day it is, so "this week", "recent" or "how long ago" would
/// be guesses. Registered for every session by the engine, like ExecuteSkill, since Puls's own sessions load only the
/// agents toolset. South African time: Puls's users, and the zone .NET knows as Africa/Johannesburg.
/// </summary>
public sealed class ClockTools(TimeProvider? time = null)
{
    private readonly TimeProvider _time = time ?? TimeProvider.System;

    // IANA name first (Linux, and Windows with ICU), then the Windows name; SAST has been UTC+2 without daylight saving since 1944.
    private static readonly TimeZoneInfo SouthAfrica = Find("Africa/Johannesburg") ?? Find("South Africa Standard Time")
        ?? TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "South Africa Standard Time", "SAST");

    [Description("Today's date and the time now in South Africa, with the weekday and the week number. Use it for anything " +
        "relative to today: 'this week', 'recent', 'how long ago', a deadline.")]
    public string GetToday()
    {
        DateTimeOffset now = TimeZoneInfo.ConvertTime(_time.GetUtcNow(), SouthAfrica);
        var invariant = CultureInfo.InvariantCulture;
        return MessageText.Format(ToolMessage.Today, new
        {
            Weekday = now.ToString("dddd", invariant),
            Date = now.ToString("d MMMM yyyy", invariant),
            Time = now.ToString("HH:mm", invariant),
            Offset = now.ToString("zzz", invariant),
            Iso = now.ToString("yyyy-MM-dd'T'HH:mm:sszzz", invariant),
            Week = ISOWeek.GetWeekOfYear(now.DateTime),
        });
    }

    private static TimeZoneInfo? Find(string id)
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException) { return null; }
    }
}
