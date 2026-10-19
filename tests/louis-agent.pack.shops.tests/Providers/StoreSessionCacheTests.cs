using LouisAgent.Packs.Shops.Providers;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Packs.Shops.Tests.Providers;

// F34, Louis's melktert chat: the cook's first search waited 12.5 s for Malmesbury's store, because the container had
// just been rebuilt and the town's store lived only in memory, per engine. With the searches now in parallel, eleven
// first searches would each have looked the town up. One lookup per town, shared, kept a day in a file that outlives
// the container.
public class StoreSessionCacheTests
{
    private static readonly BrowserSession Malmesbury = new([new BrowserCookie("storeids", "655b0c8f", "www.checkers.co.za")]);

    private string _file = null!;
    private DateTimeOffset _now;

    [SetUp]
    public void SetUp()
    {
        _file = Path.Combine(Path.GetTempPath(), $"shop-stores-{Guid.NewGuid():N}.json");
        _now = new DateTimeOffset(2026, 10, 18, 2, 20, 0, TimeSpan.Zero);
    }

    [TearDown]
    public void TearDown() => File.Delete(_file);

    private StoreSessionCache Cache() => new(_file, () => _now);

    private static Func<Task<(BrowserSession?, bool)>> Lookup(Func<int> count, BrowserSession? session = null, bool keep = true, int delayMs = 0) =>
        async () =>
        {
            count();
            if (delayMs > 0) await Task.Delay(delayMs);
            return (session, keep);
        };

    [Test]
    public async Task SearchesAskingAtOnce_ShareOneLookup()
    {
        int lookups = 0;
        var cache = Cache();

        var sessions = await Task.WhenAll(Enumerable.Range(0, 11)
            .Select(_ => cache.GetOrLookUpAsync("checkers", "Malmesbury", Lookup(() => Interlocked.Increment(ref lookups), Malmesbury, delayMs: 100))));

        Assert.That(lookups, Is.EqualTo(1));
        Assert.That(sessions, Has.All.SameAs(Malmesbury));
    }

    [Test]
    public async Task ATownsStore_OutlivesTheProcess_ForADay()
    {
        int lookups = 0;
        await Cache().GetOrLookUpAsync("checkers", "Malmesbury", Lookup(() => ++lookups, Malmesbury));

        var restarted = await Cache().GetOrLookUpAsync("checkers", " malmesbury ", Lookup(() => ++lookups, Malmesbury));
        Assert.That(lookups, Is.EqualTo(1), "read from the file, not looked up again");
        Assert.That(restarted!.Cookies.Single().Value, Is.EqualTo("655b0c8f"));

        _now = _now.AddDays(1).AddMinutes(1);
        await Cache().GetOrLookUpAsync("checkers", "Malmesbury", Lookup(() => ++lookups, Malmesbury));
        Assert.That(lookups, Is.EqualTo(2), "a day old: looked up again");
    }

    [Test]
    public async Task ATownWithNoStore_IsKept_AFailedLookupIsNot()
    {
        int lookups = 0;
        var cache = Cache();

        await cache.GetOrLookUpAsync("checkers", "Prince Albert", Lookup(() => ++lookups, session: null));
        Assert.That(await cache.GetOrLookUpAsync("checkers", "Prince Albert", Lookup(() => ++lookups, Malmesbury)), Is.Null);
        Assert.That(lookups, Is.EqualTo(1), "no store there is an answer too");

        await cache.GetOrLookUpAsync("checkers", "Darling", Lookup(() => ++lookups, session: null, keep: false));
        await cache.GetOrLookUpAsync("checkers", "Darling", Lookup(() => ++lookups, Malmesbury));
        Assert.That(lookups, Is.EqualTo(3), "a lookup that failed is tried again");
    }

    [Test]
    public async Task WithoutAFile_ItStillKeepsTownsInMemory()
    {
        int lookups = 0;
        var cache = new StoreSessionCache();

        await cache.GetOrLookUpAsync("checkers", "Malmesbury", Lookup(() => ++lookups, Malmesbury));
        await cache.GetOrLookUpAsync("checkers", "Malmesbury", Lookup(() => ++lookups, Malmesbury));

        Assert.That(lookups, Is.EqualTo(1));
    }

    [Test]
    public async Task AnUnreadableFile_IsNoCache_NotAFailedSearch()
    {
        await File.WriteAllTextAsync(_file, "{ not json");
        int lookups = 0;

        var session = await Cache().GetOrLookUpAsync("checkers", "Malmesbury", Lookup(() => ++lookups, Malmesbury));

        Assert.That((lookups, session), Is.EqualTo((1, Malmesbury)));
    }
}
