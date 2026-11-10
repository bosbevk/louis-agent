using LouisAgent.Web.Components.Site.Addresses;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Tests.Components.Site.Addresses;

/// <summary>
/// A place's own address draws its page in the page's own layout (F127.1, Louis: "when you refresh the page it loses
/// the demo container"): the router's last resort draws the page an address names, and the frame around it is that
/// page's, not the site's default.
/// </summary>
public class PlaceLayoutTests
{
    /// <summary>A page that names a layout of its own.</summary>
    [Route("/place-layout-demo")]
    [Layout(typeof(DemoFrame))]
    private sealed class DemoPage : ComponentBase;

    /// <summary>A page in the site's default frame.</summary>
    [Route("/place-layout-plain")]
    private sealed class PlainPage : ComponentBase;

    /// <summary>The demo page's own frame.</summary>
    private sealed class DemoFrame : LayoutComponentBase;

    /// <summary>The site's default frame.</summary>
    private sealed class SiteFrame : LayoutComponentBase;

    private static readonly SiteAddresses Addresses = SiteAddresses.From("home", typeof(PlaceLayoutTests).Assembly);

    /// <summary>The router's place page, at an address.</summary>
    private static RouteData AtPlace(string path) =>
        new(typeof(SectionPage), new Dictionary<string, object?> { ["Path"] = path });

    /// <summary>A place on a page with its own frame is drawn in that frame.</summary>
    [Test]
    public void APlaceOnAPageWithItsOwnFrame_IsDrawnInIt() =>
        Assert.That(PlaceLayout.Of(AtPlace("place-layout-demo/the-demo"), typeof(SiteFrame), Addresses),
            Is.EqualTo(typeof(DemoFrame)));

    /// <summary>A place on a page in the default frame, a page routed itself, or no addresses: the default.</summary>
    [Test]
    public void EverythingElse_HasTheDefault()
    {
        var page = new RouteData(typeof(DemoPage), new Dictionary<string, object?>());

        Assert.Multiple(() =>
        {
            Assert.That(PlaceLayout.Of(AtPlace("place-layout-plain/the-demo"), typeof(SiteFrame), Addresses),
                Is.EqualTo(typeof(SiteFrame)));
            Assert.That(PlaceLayout.Of(page, typeof(SiteFrame), Addresses), Is.EqualTo(typeof(SiteFrame)),
                "a page routed itself: the router reads its own layout");
            Assert.That(PlaceLayout.Of(AtPlace("place-layout-demo/the-demo"), typeof(SiteFrame), null),
                Is.EqualTo(typeof(SiteFrame)));
            Assert.That(PlaceLayout.Of(AtPlace("nowhere/at-all"), typeof(SiteFrame), Addresses),
                Is.EqualTo(typeof(SiteFrame)));
        });
    }
}
