using LouisAgent.Testing;
using LouisAgent.Packs.Shops.Places;
using LouisAgent.Packs.Shops.Providers;
using LouisAgent.Core.Tools.Web.Browsing;

namespace LouisAgent.Packs.Shops.IntegrationTests.Providers;

// F34 step 1b (Louis: "make sure we set the address in checkersprovider, to make sure we use the correct catalogue").
// Checkers answers for a default store unless the visitor has chosen an area: its own address picker asks
// /api/store/fetch-store-contexts for the area's stores, and the reply sets the cookies every later search is answered
// from. CheckersStore does the same once per town, in a visit of its own, and keeps the cookies Checkers set. The
// answer is a real one for Malmesbury's centre, recorded 2026-10-17.
[Category("Integration")]
public class CheckersStoreIntegrationTests
{
    [Test, Category("Live")]
    public async Task Malmesbury_IsServedByAStoreOtherThanTheDefault()
    {
        string url = Outside.Stack("BROWSER_URL", "browser to ask Checkers with");

        var session =
            await new CheckersStore(new ChromeWebBrowser(new Uri(url!)),
                LouisAgent.Packs.Shops.Places.PlaceCoordinates.OfAsync).ForAsync("Malmesbury");

        Assert.That(session, Is.Not.Null);
        Assert.That(session!.Cookies.Single(c => c.Name == "storeids").Value,
            Does.Not.StartWith("5ece6935faafe599532665b2"), "the default store's id");
    }
}
