using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.Clock;
using LouisAgent.Core.Sessions;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Tools.Clock;

// F22 step 1: today's date and the time where the user is, so "this week" or "recent" isn't a guess. CL15 step 3: the
// default zone is the host's (HOST_TIME_ZONE), not South Africa's; the Puls stack sets Africa/Johannesburg.
public class ClockToolsTests
{
    private const string Johannesburg = "Africa/Johannesburg";

    [Test]
    public void GetToday_IsInTheHostsZone_WithTheWeekdayAndWeek()
    {
        // 20:15 UTC on Tuesday 13 October 2026 is 22:15 in South Africa (UTC+2, no daylight saving).
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 20, 15, 0, TimeSpan.Zero));

        string today = new ClockTools(time, Johannesburg).GetToday();

        Assert.That(today, Is.EqualTo("Tuesday 13 October 2026, 22:15 in Africa/Johannesburg (UTC+02:00). ISO: 2026-10-13T22:15:00+02:00. Week 42."));
    }

    [Test]
    public void TheHostsZone_IsASetting_AndTheMachinesOwnWhenUnset()
    {
        // CL15: core names no country; an invoice clerk in Berlin gets Berlin's time (CEST, UTC+2 in October).
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 20, 15, 0, TimeSpan.Zero));

        Assert.That(new ClockTools(time, "Europe/Berlin").GetToday(), Does.StartWith("Tuesday 13 October 2026, 22:15 in Europe/Berlin"));
        Assert.That(new ClockTools(time).GetToday(), Does.Contain($" in {TimeZoneInfo.Local.Id} "), "unset: the machine's own zone");
        Assert.That(AgentOptions.FromEnvironment(name => name == "HOST_TIME_ZONE" ? " Africa/Johannesburg " : null).HostTimeZone, Is.EqualTo(Johannesburg));
        Assert.That(AgentOptions.FromEnvironment(_ => null).HostTimeZone, Is.Null);
    }

    [Test]
    public void AZoneThisMachineDoesntKnow_StopsTheHost_AtStartUp()
    {
        // Review of step 3: "Africa/Johannesberg" fell back to the machine's zone in silence and reached the model's
        // place line misspelled, two hours off on "vanaand". Every other setting refuses to start when it's wrong.
        var error = Assert.Throws<ArgumentException>(() => AgentOptions.FromEnvironment(name => name == "HOST_TIME_ZONE" ? "Africa/Johannesberg" : null))!;

        Assert.That(error.Message, Does.Contain("HOST_TIME_ZONE").And.Contain("Africa/Johannesberg"));
    }

    [Test]
    public void GetToday_CrossesMidnight_ByTheLocalClock()
    {
        // 23:30 UTC is already the next day in South Africa.
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 12, 31, 23, 30, 0, TimeSpan.Zero));

        Assert.That(new ClockTools(time, Johannesburg).GetToday(), Does.StartWith("Friday 1 January 2027, 01:30"));
    }

    [Test]
    public void GetToday_IsInTheUsersTimeZone_WhenTheyShareIt()
    {
        // F22 step 2b: London is on summer time (UTC+1) in October.
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 20, 15, 0, TimeSpan.Zero));

        string today;
        using (UserPlace.Use(new UserPlace("London, England", "Europe/London")))
            today = new ClockTools(time, Johannesburg).GetToday();

        Assert.That(today, Does.StartWith("Tuesday 13 October 2026, 21:15 in Europe/London (UTC+01:00)"));
    }

    [Test]
    public void AnUnknownTimeZone_FallsBackToTheHosts()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 10, 13, 20, 15, 0, TimeSpan.Zero));

        using (UserPlace.Use(new UserPlace("Nêrens", "Mars/Olympus")))
            Assert.That(new ClockTools(time, Johannesburg).GetToday(), Does.Contain("22:15 in Africa/Johannesburg"));
        Assert.That(UserPlace.FindTimeZone("Mars/Olympus"), Is.Null);
        Assert.That(new ClockTools(time, "Mars/Olympus").GetToday(), Does.Contain($" in {TimeZoneInfo.Local.Id} "), "a bad setting: the machine's own, not a crash");
    }
}
