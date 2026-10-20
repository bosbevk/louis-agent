using LouisAgent.Packs.Shops.Providers;

namespace LouisAgent.Packs.Shops.Tests.Providers;

// F34, cook check 42d837f13659: after about 50 searches in 7 minutes Checkers answered with a page that wasn't its
// catalogue, then nothing, for four minutes (00:24-00:28), and the cook priced 1 of 8. With searches now in parallel
// it would come sooner. A shop gets at most two searches at once, and once it refuses one, the next wait a while.
public class ShopThrottleTests
{
    private DateTimeOffset _now = new(2026, 10, 18, 0, 24, 0, TimeSpan.Zero);
    private readonly List<TimeSpan> _waits = [];

    private ShopThrottle Throttle(int atOnce = 2) => new(atOnce, TimeSpan.FromSeconds(20), () => _now,
        (wait, _) => { _waits.Add(wait); _now += wait; return Task.CompletedTask; });

    [Test]
    public async Task AtMostTwoSearches_RunAtOnce()
    {
        var throttle = Throttle();
        int running = 0, most = 0;

        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => throttle.RunAsync(async () =>
        {
            int now = Interlocked.Increment(ref running);
            lock (_waits) most = Math.Max(most, now);
            await Task.Delay(50);
            Interlocked.Decrement(ref running);
            return 0;
        })));

        Assert.That(most, Is.EqualTo(2));
    }

    [Test]
    public async Task AfterTheShopRefusesOne_TheNextWaitsItsCooldown()
    {
        var throttle = Throttle();
        await throttle.RunAsync(() => Task.FromResult(0));
        Assert.That(_waits, Is.Empty, "no refusal, no wait");

        throttle.Refused();
        _now += TimeSpan.FromSeconds(5);
        await throttle.RunAsync(() => Task.FromResult(0));

        Assert.That(_waits, Is.EqualTo(new[] { TimeSpan.FromSeconds(15) }), "what's left of the 20 s");
        await throttle.RunAsync(() => Task.FromResult(0));
        Assert.That(_waits, Has.Count.EqualTo(1), "the cooldown is over");
    }
}
