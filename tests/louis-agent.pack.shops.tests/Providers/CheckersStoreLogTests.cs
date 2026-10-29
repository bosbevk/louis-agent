using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Packs.Shops.Providers;

namespace LouisAgent.Packs.Shops.Tests.Providers;

// F18 step 4f.2: the store lookup's log lines say which shop and what happened, and show the town, its centre's coordinates and the
// store ids as keyed hashes and lengths: a store's id names its town as surely as the coordinates do, and the town is what the user
// told us. The plain values stay out of stderr in every outcome of the lookup.
[NonParallelizable]
public class CheckersStoreLogTests
{
    private const string Town = "Malmesbury, Wes-Kaap";

    private static readonly string Contexts = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "tests",
        "louis-agent.pack.shops.tests", "Providers", "Fixtures", "checkers-store-contexts.json"));

    private static readonly BrowserCookie[] SetByCheckers =
    [
        new("storeContexts", "%5B%7B%22storeId%22%3A%22655b0c8fb44cda100fd99383%22%7D%5D", "www.checkers.co.za"),
        new("storeids", "6a60644dcd48d5c3791143cf-655b0c8fb44cda100fd99383", "www.checkers.co.za"),
        new("istio-storeIds", "6a60644dcd48d5c3791143cf-655b0c8fb44cda100fd99383", "www.checkers.co.za"),
    ];

    private sealed class FakeBrowser(string? answer = null, Exception? fails = null) : IWebBrowser
    {
        public Task<BrowserVisit> VisitAsync(Uri url, Func<Uri, bool>? capture = null, BrowserSession? session = null,
            string? script = null,
            string? readySelector = null, CancellationToken cancellationToken = default) =>
            fails is not null
                ? throw fails
                : Task.FromResult(new BrowserVisit(url.AbsoluteUri, "<html></html>", [], answer, SetByCheckers));
    }

    private static Task<(double Latitude, double Longitude)?> Centre(string _, CancellationToken __) =>
        Task.FromResult<(double, double)?>((-33.460278, 18.723611));

    private static readonly string[] Private =
    [
        "Malmesbury", "Wes-Kaap", "-33.46", "33.4603", "18.72", "6a60644dcd48d5c3791143cf", "655b0c8fb44cda100fd99383"
    ];

    private static async Task<string> LogOfAsync(IWebBrowser browser)
    {
        using var stderr = new CapturedStderr();
        await new CheckersStore(browser, Centre, new StoreSessionCache(),
            new ShopThrottle(2, TimeSpan.Zero, null, (_, _) => Task.CompletedTask)).ForAsync(Town);
        return stderr.Text;
    }

    private static void AssertNoneOfThePrivateValues(string log)
    {
        foreach (string value in Private) Assert.That(log, Does.Not.Contain(value), $"'{value}' reached the log");
    }

    [Test]
    public async Task AStoreForTheTown_LogsTheTownTheCentreAndTheStoreIdsAsHashes()
    {
        string log = await LogOfAsync(new FakeBrowser(Contexts));

        AssertNoneOfThePrivateValues(log);
        Assert.That(log, Does.Contain("Checkers serves #").And.Contain($"({Town.Length})"),
            "the shop and what happened stay; the town is a hash and its length");
        Assert.That(log, Does.Contain("(its centre #").And.Contain("from stores #"));
    }

    [Test]
    public async Task ATownWithNoStore_LogsItsTownAndCentreAsHashes()
    {
        string log = await LogOfAsync(new FakeBrowser("{\"storeContexts\":[],\"servicedByOtherBrand\":null}"));

        AssertNoneOfThePrivateValues(log);
        Assert.That(log, Does.Contain("Checkers names no store for #").And.Contain("(its centre #"));
    }

    [Test]
    public async Task ALookupThatFails_LogsTheTownAndItsReasonAsHashes()
    {
        string log =
            await LogOfAsync(new FakeBrowser(fails: new WebBrowserException($"The page for {Town} did not load.")));

        AssertNoneOfThePrivateValues(log);
        Assert.That(log,
            Does.Contain("Checkers couldn't find the store for #").And.Contain("the default store answers"));
    }

    [Test]
    public async Task AnAnswerThatIsNotTheStoreAnswer_LogsTheTownAsAHash()
    {
        string log = await LogOfAsync(new FakeBrowser("<html><body>Request blocked</body></html>"));

        AssertNoneOfThePrivateValues(log);
        Assert.That(log, Does.Contain("Checkers didn't answer the store lookup for #"));
    }
}
