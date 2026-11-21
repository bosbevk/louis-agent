using LouisAgent.Web.Chats;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Web.Tests.Chats;

/// <summary>
/// A stream's updates come in bursts with long gaps between them, while a tool runs or the model thinks. The page
/// redraws at most every 50 ms, and the last update of a burst still has to reach the screen, or the page sits behind
/// for the whole gap and then seems to print everything at once (FX58).
/// </summary>
public class RedrawThrottleTests
{
    private static readonly TimeSpan Window = TimeSpan.FromMilliseconds(50);

    /// <summary>A throttle on a clock the test moves, and how many times it has drawn.</summary>
    private sealed class Harness
    {
        public readonly FakeTimeProvider Clock = new(new DateTimeOffset(2026, 11, 6, 12, 0, 0, TimeSpan.Zero));
        public readonly RedrawThrottle Throttle;
        public int Draws;

        public Harness() => Throttle = new RedrawThrottle(Clock, Window, () => Draws++);

        /// <summary>Moves the clock by this many milliseconds.</summary>
        public void Wait(int milliseconds) => Clock.Advance(TimeSpan.FromMilliseconds(milliseconds));
    }

    /// <summary>The first update shows at once.</summary>
    [Test]
    public void TheFirstUpdate_ShowsAtOnce()
    {
        var h = new Harness();

        h.Throttle.Request();

        Assert.That(h.Draws, Is.EqualTo(1));
    }

    /// <summary>The bug: an update just after a redraw, with nothing behind it, shows when the window ends.</summary>
    [Test]
    public void TheLastUpdateOfABurst_ShowsWhenTheWindowEnds_WithNothingBehindIt()
    {
        var h = new Harness();
        h.Throttle.Request();
        h.Wait(10);
        h.Throttle.Request();
        Assert.That(h.Draws, Is.EqualTo(1), "not yet: the window is still open");

        h.Wait(40);

        Assert.That(h.Draws, Is.EqualTo(2), "drawn with no further event to carry it");
    }

    /// <summary>However many updates land in a window, the page redraws once for them.</summary>
    [Test]
    public void ManyUpdatesInAWindow_RedrawOnce()
    {
        var h = new Harness();
        h.Throttle.Request();

        for (int i = 0; i < 4; i++)
        {
            h.Wait(5);
            h.Throttle.Request();
        }

        h.Wait(1000);

        Assert.That(h.Draws, Is.EqualTo(2));
    }

    /// <summary>A catch-up starts a new window, so an update right after it waits its turn.</summary>
    [Test]
    public void ACatchUp_StartsANewWindow()
    {
        var h = new Harness();
        h.Throttle.Request();
        h.Wait(10);
        h.Throttle.Request();
        h.Wait(40);

        h.Wait(10);
        h.Throttle.Request();
        Assert.That(h.Draws, Is.EqualTo(2), "still inside the catch-up's window");

        h.Wait(40);
        Assert.That(h.Draws, Is.EqualTo(3));
    }

    /// <summary>An update after the window shows at once, and no catch-up follows it.</summary>
    [Test]
    public void AnUpdateAfterTheWindow_ShowsAtOnce_AndNoCatchUpFollows()
    {
        var h = new Harness();
        h.Throttle.Request();
        h.Wait(60);

        h.Throttle.Request();
        h.Wait(1000);

        Assert.That(h.Draws, Is.EqualTo(2));
    }

    /// <summary>When the turn ends, a waiting catch-up is dropped: the runner draws the end itself.</summary>
    [Test]
    public void Stop_DropsTheWaitingCatchUp()
    {
        var h = new Harness();
        h.Throttle.Request();
        h.Wait(10);
        h.Throttle.Request();

        h.Throttle.Stop();
        h.Wait(1000);

        Assert.That(h.Draws, Is.EqualTo(1));
    }
}
