using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Tools.Clock;

// F22 step 1: today's date and the time in South Africa, so "this week" or "recent" isn't a guess.
public class ClockToolsTests
{
    [Test]
    public void GetToday_IsSouthAfricanTime_WithTheWeekdayAndWeek()
    {
        // 20:15 UTC on Tuesday 13 October 2026 is 22:15 in South Africa (UTC+2, no daylight saving).
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 20, 15, 0, TimeSpan.Zero));

        string today = new ClockTools(time).GetToday();

        Assert.That(today, Is.EqualTo("Tuesday 13 October 2026, 22:15 South African time (UTC+02:00). ISO: 2026-10-13T22:15:00+02:00. Week 42."));
    }

    [Test]
    public void GetToday_CrossesMidnight_ByTheLocalClock()
    {
        // 23:30 UTC is already the next day in South Africa.
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero));

        Assert.That(new ClockTools(time).GetToday(), Does.StartWith("Friday 1 January 2027, 01:30"));
    }
}
