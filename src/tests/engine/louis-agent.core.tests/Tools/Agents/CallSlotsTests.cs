using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Tools.Agents;

// S (CL15): the sub-session calls a turn may make, with the clock injected: so many per turn, and outside a turn (an
// MCP call, PR #27 review) so many a minute, never one key that fills up forever.
public class CallSlotsTests
{
    [Test]
    public void InATurn_TheFourthCallIsRefused_AndANewTurnStartsAgain()
    {
        var slots = new CallSlots(maxPerTurn: 3, noTurnWindow: TimeSpan.FromMinutes(1), TimeProvider.System);

        using (UsageScope.Begin(session: "sess_parent", turn: 2))
        {
            for (int i = 0; i < 3; i++) Assert.That(slots.Take(UsageScope.Current, out string? parent), Is.Null, $"call {i + 1}, parent {parent}");
            Assert.That(slots.Take(UsageScope.Current, out string? tag), Does.Contain("already made 3 sub-session calls"));
            Assert.That(tag, Is.EqualTo("sess_parent#2"));
        }

        using (UsageScope.Begin(session: "sess_parent", turn: 3))
            Assert.That(slots.Take(UsageScope.Current, out _), Is.Null);
    }

    [Test]
    public void OutsideATurn_TheLimitIsPerMinute_NotForever()
    {
        var time = new FakeTimeProvider(DateTimeOffset.Parse("2026-10-13T10:00:00Z"));
        var slots = new CallSlots(maxPerTurn: 3, noTurnWindow: TimeSpan.FromMinutes(1), time);

        for (int i = 0; i < 3; i++) Assert.That(slots.Take(null, out string? parent), Is.Null, $"call {i + 1}, parent {parent}");
        Assert.That(slots.Take(null, out string? tag), Does.Contain("in the last minute"));
        Assert.That(tag, Is.Null, "no turn, no parent tag");

        time.Advance(TimeSpan.FromMinutes(1));
        Assert.That(slots.Take(null, out _), Is.Null, "a minute later it works again");
    }

    [Test]
    public void TheParentTag_IsTheSessionAndTurn()
    {
        using (UsageScope.Begin(session: "sess_1", turn: 7))
            Assert.That(CallSlots.ParentTag(UsageScope.Current), Is.EqualTo("sess_1#7"));
        Assert.That(CallSlots.ParentTag(null), Is.Null);
    }
}
